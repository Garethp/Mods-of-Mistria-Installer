//! Executes a GML script on the pinned fabricator VM.
//!
//! The execute-capable sibling of `momi-gml-check`, and the split is the
//! whole point. `momi-gml-check` is what the installer invokes on user mod
//! code: it compiles and never executes, which is a safety property of that
//! binary rather than a missing feature. This one executes, so it is a
//! separate binary that CI builds for the test suite and the release pipeline
//! never bundles (it bundles `momi-gml-check` by name).
//!
//! Its consumers are the mmapi runtime suites and the engine-claim probes,
//! which run the carried framework against the same VM rev the compile gate
//! pins. Before this binary existed those suites needed a full fabricator
//! checkout, so they skipped on a plain clone and in CI - a regression net that
//! was never actually armed.
//!
//! Behaviour mirrors `fabricator-cli`'s `interpreter run` at the pinned rev,
//! because the test may use either as its backend and they must agree: same
//! dialect selection (by file extension), same optimization level, same
//! stdlib + magic imports. The script's own output (`show_debug_message`)
//! goes to stdout; every diagnostic goes to stderr, so a caller can parse
//! stdout as probe output alone.

use std::{env, fs, path::PathBuf, process::ExitCode};

use fabricator_compiler::{CompileSettings, Compiler, ImportItems};
use fabricator_stdlib::StdlibContext as _;
use fabricator_vm as vm;

mod rev;

/// `fabricator-cli`'s interpreter defaults to 2, and a probe must not behave
/// differently depending on which backend ran it.
const OPT_LEVEL: u8 = 2;

const USAGE: &str = "\
usage: momi-gml-probe run <path>
       momi-gml-probe --version

Compiles <path> and executes it on the pinned fabricator VM. The script's own
output (show_debug_message) goes to stdout; diagnostics go to stderr. Exit
codes: 0 ran to completion, 1 compile error or uncaught runtime error, 2
usage or I/O error.";

fn usage_error(msg: &str) -> ExitCode {
    eprintln!("momi-gml-probe: {msg}");
    eprintln!("{USAGE}");
    ExitCode::from(2)
}

/// Compiles `path` and runs it to completion. A script that throws past its
/// own handlers is a failure, not a crash of this binary.
fn run(path: &PathBuf) -> ExitCode {
    // Decoded lossily to match momi-gml-check: a stray byte should surface as
    // a compile error against the file, not an I/O error about the tool.
    let code = match fs::read(path) {
        Ok(bytes) => String::from_utf8_lossy(&bytes).into_owned(),
        Err(err) => {
            eprintln!("{}: {}", path.display(), err);
            return ExitCode::from(2);
        }
    };

    let interpreter = vm::Interpreter::new();
    interpreter.enter(|ctx| {
        let output = match Compiler::compile_chunk(
            ctx,
            "default",
            ImportItems::with_magic(&ctx, ctx.stdlib()),
            CompileSettings::from_path(path).set_optimization_passes(OPT_LEVEL),
            path.to_string_lossy().into_owned(),
            &code,
        ) {
            Ok(output) => output,
            Err(err) => {
                eprintln!("{}: {}", path.display(), err);
                return ExitCode::FAILURE;
            }
        };

        let closure = match vm::Closure::new(&ctx, output.chunk_prototype, vm::Value::Undefined) {
            Ok(closure) => closure,
            Err(err) => {
                eprintln!("{}: {}", path.display(), err);
                return ExitCode::FAILURE;
            }
        };

        match vm::Thread::new(&ctx).run(ctx, closure) {
            Ok(()) => ExitCode::SUCCESS,
            Err(err) => {
                eprintln!("{}: {}", path.display(), err);
                ExitCode::FAILURE
            }
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
            "momi-gml-probe {} (fabricator {})",
            env!("CARGO_PKG_VERSION"),
            rev::FABRICATOR_REV
        );
        return ExitCode::SUCCESS;
    }

    if mode != "run" {
        return usage_error(&format!("unknown mode `{mode}`"));
    }
    match args.len() {
        1 => usage_error("run requires a path"),
        2 => run(&PathBuf::from(&args[1])),
        _ => usage_error("run takes exactly one path"),
    }
}
