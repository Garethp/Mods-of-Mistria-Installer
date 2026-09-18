using Garethp.ModsOfMistriaInstallerLib.Seam;

namespace ModsOfMistriaInstallerLibTests.Seam;

[TestFixture]
public class PayloadReadsTest
{
    [Test]
    public void ShouldExtractTheFilteredVarAndCtxRoots()
    {
        var payload = "    try { spd = mmapi_apply_filters(\"player.speed\", spd, "
                      + "{ player: self, cap: max_spd }); } catch (__mmapi_t) {}\n";

        Assert.That(PayloadReads.ScopeReads(payload), Is.EqualTo(new[] { "spd", "max_spd" }));
    }

    [Test]
    public void ShouldExtractTheRootOfAMemberChain()
    {
        var payload = "    try { mmapi_emit(\"a.b\", node.renderer.x); } catch (__mmapi_t) {}\n";

        Assert.That(PayloadReads.ScopeReads(payload), Is.EqualTo(new[] { "node" }));
    }

    [Test]
    public void ShouldExtractOnVetoReads()
    {
        var payload = "    try { if (mmapi_check_guards(\"a.b\", undefined) == false) "
                      + "{ return fallback; } } catch (__mmapi_t) {}\n";

        Assert.That(PayloadReads.ScopeReads(payload), Is.EqualTo(new[] { "fallback" }));
    }

    [Test]
    public void ShouldSkipNamesBoundOutsideTheEnclosingScope()
    {
        // keywords, uppercase engine globals, member names, call names,
        // struct keys, string contents, numbers and the framework's own
        // names all bind elsewhere
        var payload = "    try { mmapi_emit(\"a.b\", { kind: ItemKind.Weapon, "
                      + "count: total_count(), flag: true, label: \"weapon label\", limit: 100 }); } "
                      + "catch (__mmapi_t) {}\n";

        Assert.That(PayloadReads.ScopeReads(payload), Is.Empty);
    }

    [Test]
    public void ShouldSkipNamesThePayloadItselfDeclares()
    {
        // the ctx_filter shape declares its pack variable and its catch
        // variables, and writes fields back to enclosing locals
        var payload = "    var __pack = {\n"
                      + "        amount: amount,\n"
                      + "    };\n"
                      + "    try { __pack = mmapi_apply_filters(\"a.b\", __pack, undefined); } catch (__t) {}\n"
                      + "    if (__pack != undefined) {\n"
                      + "        try { amount = __pack.amount; } catch (__t_amount) {}\n"
                      + "    }\n";

        Assert.That(PayloadReads.ScopeReads(payload), Is.EqualTo(new[] { "amount" }));
    }

    [Test]
    public void ShouldSkipAssetPrefixedNames()
    {
        var payload = "    try { mmapi_emit(\"a.b\", { icon: spr_icon_big, door: obj_door, "
                      + "dest: rm_hall, item: item }); } catch (__mmapi_t) {}\n";

        Assert.That(PayloadReads.ScopeReads(payload), Is.EqualTo(new[] { "item" }));
    }

    [Test]
    public void ShouldCollectAssetReadsInValuePosition()
    {
        // asset names are exempt from scope reads but collected here under the
        // same occurrence rules, so value positions count and struct keys,
        // member names and call names do not
        var payload = "    try { mmapi_emit(\"a.b\", { icon: spr_icon_big, door: obj_door, "
                      + "dest: rm_hall, spr_key_lookalike: item, thing: box.spr_member }); } "
                      + "catch (__mmapi_t) {}\n";

        Assert.That(PayloadReads.AssetReads(payload),
            Is.EqualTo(new[] { "spr_icon_big", "obj_door", "rm_hall" }));
    }

    [Test]
    public void ShouldSkipABareAssignmentTargetButKeepOtherOccurrences()
    {
        // a bare assignment creates the member it writes. The same name in
        // read position elsewhere still counts, which keeps a filtered
        // variable protected.
        var payload = "    icon_width = sprite_get_width(sprite_index);\n"
                      + "    try { amount = mmapi_apply_filters(\"a.b\", amount, undefined); } catch (__t) {}\n";

        Assert.That(PayloadReads.ScopeReads(payload), Is.EqualTo(new[] { "sprite_index", "amount" }));
    }

    [Test]
    public void ShouldCollectDeclarations()
    {
        var payload = "    var pack = {};\n"
                      + "    outline = thing.get();\n"
                      + "    self.member = 1;\n"
                      + "    try { x(); } catch (err) {}\n";

        Assert.That(PayloadReads.Declarations(payload), Is.EquivalentTo(new[] { "pack", "outline", "err" }));
    }

    [Test]
    public void ShouldSkipACustomCatchVariable()
    {
        var payload = "    try { mmapi_emit(\"a.b\", item); } catch (save_err) {}\n";

        Assert.That(PayloadReads.ScopeReads(payload), Is.EqualTo(new[] { "item" }));
    }
}
