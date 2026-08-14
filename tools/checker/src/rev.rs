//! The fabricator pin, shared by both binaries in this crate.
//!
//! The authoritative pin is the `rev` on each fabricator dependency in
//! Cargo.toml; this is the string those binaries *report*. It lives here so
//! adding a binary does not add another copy to drift out of step. A test
//! asserts this string and the four dependency revs all agree.

/// The fabricator commit this crate is built against (see Cargo.toml).
pub const FABRICATOR_REV: &str = "f2483a61d53c730020f2357c790603dd6362476b";
