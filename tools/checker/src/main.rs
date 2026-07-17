//! Compile-checks GML/FML sources with the fabricator compiler without
//! running them. MOMI ships this prebuilt so end users get compile
//! validation at apply time with no Rust toolchain.

use std::{env, fs, path::PathBuf, process::ExitCode};

use fabricator_compiler::{CompileSettings, Compiler, ImportItems};
use fabricator_stdlib::StdlibContext as _;
use fabricator_vm as vm;

mod rev;

const USAGE: &str = "\
usage: momi-gml-check files [--files-from <listfile>] [paths...]
       momi-gml-check unit  [--files-from <listfile>] [paths...]
       momi-gml-check --version

`files` compiles each path as an independent chunk. `unit` compiles all
paths together as one compilation unit, which additionally catches
cross-chunk duplicate top-level function exports. `--files-from` reads one
path per line (blank lines ignored). Exit codes: 0 all compiled, 1 any
compile failure, 2 usage or I/O error.";

fn usage_error(msg: &str) -> ExitCode {
    eprintln!("momi-gml-check: {msg}");
    eprintln!("{USAGE}");
    ExitCode::from(2)
}

/// Reads a file as bytes and decodes UTF-8 lossily, since mod files may
/// contain stray bytes.
fn read_source(path: &PathBuf) -> Result<String, std::io::Error> {
    Ok(String::from_utf8_lossy(&fs::read(path)?).into_owned())
}

/// Compiles each source as its own independent chunk, reporting every
/// failure rather than stopping at the first.
fn check_files(sources: &[(PathBuf, String)]) -> ExitCode {
    let interpreter = vm::Interpreter::new();
    interpreter.enter(|ctx| {
        let mut failed = false;
        for (path, code) in sources {
            let result = Compiler::compile_chunk(
                ctx,
                "default",
                ImportItems::with_magic(&ctx, ctx.stdlib()),
                CompileSettings::from_path(path),
                path.to_string_lossy().into_owned(),
                code,
            );
            if let Err(err) = result {
                eprintln!("{}: {}", path.display(), err);
                failed = true;
            }
        }
        if failed {
            ExitCode::FAILURE
        } else {
            ExitCode::SUCCESS
        }
    })
}

/// Compiles every source together as one compilation unit, which catches
/// cross-chunk duplicate exports and stdlib shadowing. Per-chunk errors are
/// all reported; the unit-wide compile only runs if every chunk was added.
fn check_unit(sources: &[(PathBuf, String)]) -> ExitCode {
    let interpreter = vm::Interpreter::new();
    interpreter.enter(|ctx| {
        let mut com = Compiler::new(
            ctx,
            "default",
            ImportItems::with_magic(&ctx, ctx.stdlib()),
        );
        let mut failed = false;
        for (path, code) in sources {
            if let Err(err) = com.add_chunk(
                CompileSettings::from_path(path),
                path.to_string_lossy().into_owned(),
                code,
            ) {
                eprintln!("{}: {}", path.display(), err);
                failed = true;
            }
        }
        if !failed {
            if let Err(err) = com.compile() {
                // The error's chunk name is the path the chunk was added as.
                eprintln!("{}: {}", err.chunk_name, err);
                failed = true;
            }
        }
        if failed {
            ExitCode::FAILURE
        } else {
            ExitCode::SUCCESS
        }
    })
}

fn main() -> ExitCode {
    let args: Vec<String> = env::args().skip(1).collect();
    let Some(mode) = args.first() else {
        return usage_error("missing mode");
    };

    if mode == "--version" {
        println!(
            "momi-gml-check {} (fabricator {})",
            env!("CARGO_PKG_VERSION"),
            rev::FABRICATOR_REV
        );
        return ExitCode::SUCCESS;
    }

    let unit = match mode.as_str() {
        "files" => false,
        "unit" => true,
        other => return usage_error(&format!("unknown mode `{other}`")),
    };

    // Gather paths from positional args and any --files-from list files.
    let mut paths: Vec<PathBuf> = Vec::new();
    let mut i = 1;
    while i < args.len() {
        let arg = &args[i];
        if arg == "--files-from" {
            i += 1;
            let Some(listfile) = args.get(i) else {
                return usage_error("--files-from requires a path");
            };
            let text = match fs::read(listfile) {
                Ok(bytes) => String::from_utf8_lossy(&bytes).into_owned(),
                Err(err) => {
                    eprintln!("{listfile}: {err}");
                    return ExitCode::from(2);
                }
            };
            // One path per line; blank (or whitespace-only) lines ignored.
            for line in text.lines() {
                let line = line.trim();
                if !line.is_empty() {
                    paths.push(PathBuf::from(line));
                }
            }
        } else if arg.starts_with("--") {
            return usage_error(&format!("unknown option `{arg}`"));
        } else {
            paths.push(PathBuf::from(arg));
        }
        i += 1;
    }

    // Read all sources up front. Every unreadable file is reported, then the
    // whole run is treated as an I/O error.
    let mut sources: Vec<(PathBuf, String)> = Vec::new();
    let mut io_failed = false;
    for path in paths {
        match read_source(&path) {
            Ok(code) => sources.push((path, code)),
            Err(err) => {
                eprintln!("{}: {}", path.display(), err);
                io_failed = true;
            }
        }
    }
    if io_failed {
        return ExitCode::from(2);
    }

    if unit {
        // Deterministic chunk order: lexicographic by path.
        sources.sort_by(|a, b| a.0.as_os_str().cmp(b.0.as_os_str()));
        check_unit(&sources)
    } else {
        check_files(&sources)
    }
}
