using Garethp.ModsOfMistriaInstallerLib.Seam;

namespace ModsOfMistriaInstallerLibTests.Seam;

[TestFixture]
public class DispatchRendererTest
{
    private static PayloadOptions Options => new("test.hook", 4, "mmapi_x", "__mmapi_x");

    [Test]
    public void ShouldRenderAnEmitPayload()
    {
        var payload = DispatchRenderer.RenderPayload(DispatchOp.Emit, Options with { Ctx = "self" });

        Assert.That(payload, Is.EqualTo(
            "    try { mmapi_emit(\"test.hook\", self); } catch (__mmapi_x) {} // mmapi_x\n"));
    }

    [Test]
    public void ShouldRenderTheEmitStructFormUnderCtxFields()
    {
        var payload = DispatchRenderer.RenderPayload(DispatchOp.Emit, Options with
        {
            CtxFields = [("npc", "self"), ("item", "item")],
        });

        Assert.That(payload, Is.EqualTo(
            "    try {\n"
            + "        mmapi_emit(\"test.hook\", { // mmapi_x\n"
            + "            npc: self,\n"
            + "            item: item,\n"
            + "        });\n"
            + "    } catch (__mmapi_x) {}\n"));
    }

    [Test]
    public void ShouldRenderAGuardPayload()
    {
        var payload = DispatchRenderer.RenderPayload(DispatchOp.Guard, Options with
        {
            Ctx = "item",
            OnVeto = "return false;",
        });

        Assert.That(payload, Is.EqualTo(
            "    try { if (mmapi_check_guards(\"test.hook\", item) == false) { return false; } } "
            + "catch (__mmapi_x) {} // mmapi_x\n"));
    }

    [Test]
    public void ShouldRenderAFilterPayload()
    {
        var payload = DispatchRenderer.RenderPayload(DispatchOp.Filter, Options with
        {
            Var = "damage",
            Ctx = "self",
        });

        Assert.That(payload, Is.EqualTo(
            "    try { damage = mmapi_apply_filters(\"test.hook\", damage, self); } "
            + "catch (__mmapi_x) {} // mmapi_x\n"));
    }

    [Test]
    public void ShouldRenderAFilterCallPayload()
    {
        var payload = DispatchRenderer.RenderPayload(DispatchOp.FilterCall, Options with
        {
            Value = "{ list: items }",
        });

        Assert.That(payload, Is.EqualTo(
            "    try { mmapi_apply_filters(\"test.hook\", { list: items }, undefined); } "
            + "catch (__mmapi_x) {} // mmapi_x\n"));
    }

    [Test]
    public void ShouldRenderACtxFilterPayload()
    {
        var payload = DispatchRenderer.RenderPayload(DispatchOp.CtxFilter, Options with
        {
            CtxVar = "__ctx",
            CtxFields = [("hp", "hp"), ("max", "max_hp")],
            Writeback = ["hp"],
        });

        Assert.That(payload, Is.EqualTo(
            "    var __ctx = {\n"
            + "        hp: hp,\n"
            + "        max: max_hp,\n"
            + "    };\n"
            + "    try { __ctx = mmapi_apply_filters(\"test.hook\", __ctx, undefined); } "
            + "catch (__mmapi_x) {} // mmapi_x\n"
            + "    if (__ctx != undefined) {\n"
            + "        try { hp = __ctx.hp; } catch (__mmapi_x_hp) {}\n"
            + "    }\n"));
    }

    [Test]
    public void ShouldSuppressTheTagWhenMarkerAndCatchVarCoincide()
    {
        var payload = DispatchRenderer.RenderPayload(DispatchOp.Emit,
            new PayloadOptions("test.hook", 4, "__mmapi_x", "__mmapi_x"));

        Assert.That(payload, Is.EqualTo(
            "    try { mmapi_emit(\"test.hook\", undefined); } catch (__mmapi_x) {}\n"));
    }

    [Test]
    public void ShouldRenderWithoutTryCatch()
    {
        var payload = DispatchRenderer.RenderPayload(DispatchOp.Emit, Options with { TryCatch = false });

        Assert.That(payload, Is.EqualTo("    mmapi_emit(\"test.hook\", undefined); // mmapi_x\n"));
    }

    [Test]
    public void ShouldPadThePayloadWithBlankLines()
    {
        var payload = DispatchRenderer.RenderPayload(DispatchOp.Emit, Options with
        {
            BlankBefore = true,
            BlankAfter = true,
        });

        Assert.That(payload, Is.EqualTo(
            "\n    try { mmapi_emit(\"test.hook\", undefined); } catch (__mmapi_x) {} // mmapi_x\n\n"));
    }

    [Test]
    public void ShouldRenderADeclFormWrapper()
    {
        var wrapper = DispatchRenderer.RenderWrap(FunctionForm.Decl, "damage", "amount, source",
            ["amount", "source"], "", "FILTER_LINE");

        Assert.That(wrapper, Is.EqualTo(
            "function damage(amount, source) {\n"
            + "    var __mmapi_wrap_result = __mmapi_orig_damage(amount, source);\n"
            + "    FILTER_LINE\n"
            + "    return __mmapi_wrap_result;\n"
            + "}\n"));
    }

    [Test]
    public void ShouldRenderAStaticFormWrapperWithASelfReceiver()
    {
        var wrapper = DispatchRenderer.RenderWrap(FunctionForm.Static, "describe", "kind, label=\"x\"",
            ["kind", "label"], "    ", "FILTER_LINE");

        Assert.That(wrapper, Is.EqualTo(
            "    static describe = function(kind, label=\"x\") {\n"
            + "        var __mmapi_wrap_result = self.__mmapi_orig_describe(kind, label);\n"
            + "        FILTER_LINE\n"
            + "        return __mmapi_wrap_result;\n"
            + "    }\n"));
    }

    [Test]
    public void ShouldRenderAnAssignFormWrapperWithASelfReceiver()
    {
        var wrapper = DispatchRenderer.RenderWrap(FunctionForm.Assign, "on_free", "", [], "    ",
            "FILTER_LINE", blankBefore: true);

        Assert.That(wrapper, Is.EqualTo(
            "\n"
            + "    on_free = function() {\n"
            + "        var __mmapi_wrap_result = self.__mmapi_orig_on_free();\n"
            + "        FILTER_LINE\n"
            + "        return __mmapi_wrap_result;\n"
            + "    }\n"));
    }
}
