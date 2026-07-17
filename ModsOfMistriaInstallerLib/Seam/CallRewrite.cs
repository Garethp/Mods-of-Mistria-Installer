namespace Garethp.ModsOfMistriaInstallerLib.Seam;

// A [[call_rewrite]]: a token-level redirect of every direct call to a native
// builtin (no GML body, so no anchored form can reach inside it) into a
// framework wrapper. It names no file and carries no anchor - it applies
// wherever the callee is called, across the whole engine tree, after every
// anchored edit. The arity, residual and at-least-one checks at stage time
// are its fail-closed contract.
public record CallRewrite(
    string Id,
    string Callee,                 // the identifier rewritten in call position
    string To,                     // the replacement identifier (a framework wrapper)
    int Args,                      // required arity of every rewritten call
    IReadOnlyList<string> Hooks);  // provides[] hook names, dispatched by the wrapper
