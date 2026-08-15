using System.Text;
using Garethp.ModsOfMistriaInstallerLib.Seam;

namespace ModsOfMistriaInstallerLibTests.Seam;

// [[extension]] parse round-trip and every catalog validation, batched the way
// SeamCatalogLoaderTest drives the seam rules.
[TestFixture]
public class ExtensionCatalogLoaderTest
{
    // no [[hook]] stanzas. An extension point declares none, and a hook with
    // no seam behind it is itself a catalog error, which would mask these
    private const string Base = "version = 2\n";

    // the npc_roster shape, trimmed to what the loader cares about
    private const string GoodPoint = "\n" + """
        [[extension]]
        id   = "roster"
        file = "gml/NpcId.gml"
        doc  = "One registration per custom NPC."

        [extension.ordinal]
        enum     = "NpcId"
        sentinel = "LEN"

        [[extension.fields]]
        name = "object"
        type = "identifier"
        doc  = "GML object name for this NPC."

        [[extension.sites]]
        id       = "enum_member"
        kind     = "enum_member"
        template = "{{symbol}} = {{ordinal}},"
        indent   = 4

        [[extension.sites]]
        id       = "id_to_obj"
        kind     = "anchor"
        anchor   = "        default: return undefined;"
        place    = "before"
        template = "case NpcId.{{symbol}}: return {{object}};"
        indent   = 8

        [[extension.sites]]
        id       = "object_macro"
        kind     = "append"
        file     = "gml/object_manifest.gml"
        template = '#macro {{object}} object("{{object}}")'

        [extension.vacancy]
        enum_member  = "{{symbol}} = {{ordinal}},"
        id_to_obj    = "case NpcId.{{symbol}}: return obj_vacant;"
        object_macro = ""

        [[extension.vacancy_files]]
        path    = "fiddle/npcs/{{symbol}}.toml"
        content = "name = \"Departed Villager\"\n"

        [[extension.companions]]
        path  = "fiddle/npcs/{{symbol}}.toml"
        level = "error"
        doc   = "The NPC prototype. Absent, the game crashes during Setup."
        """ + "\n";

    private static SeamCatalog Load(string text) =>
        SeamCatalogLoader.Load(Encoding.UTF8.GetBytes(text), "seams.toml");

    private static string ErrorsFor(string text)
    {
        var exception = Assert.Throws<SeamCatalogException>(() => Load(text));
        return exception!.Message;
    }

    // Replace one key's value in the fixture, for the single-variable cases below.
    private static string With(string find, string replace) => Base + GoodPoint.Replace(find, replace);

    [Test]
    public void ShouldRoundTripAPoint()
    {
        var point = Load(Base + GoodPoint).Extensions.Single();

        Assert.That(point.Id, Is.EqualTo("roster"));
        Assert.That(point.File, Is.EqualTo("assets/gml/NpcId.gml"));
        Assert.That(point.OrdinalEnum, Is.EqualTo("NpcId"));
        Assert.That(point.OrdinalSentinel, Is.EqualTo("LEN"));
        Assert.That(point.Fields.Single().Name, Is.EqualTo("object"));
        Assert.That(point.Fields.Single().Type, Is.EqualTo(ExtensionFieldType.Identifier));
        Assert.That(point.Sites.Select(s => s.Id),
            Is.EqualTo(new[] { "enum_member", "id_to_obj", "object_macro" }));
    }

    [Test]
    public void ShouldDefaultASiteFileToThePointFileAndHonourAnOverride()
    {
        var point = Load(Base + GoodPoint).Extensions.Single();

        Assert.That(point.Sites[0].File, Is.EqualTo("assets/gml/NpcId.gml"));
        Assert.That(point.Sites[1].File, Is.EqualTo("assets/gml/NpcId.gml"));
        Assert.That(point.Sites[2].File, Is.EqualTo("assets/gml/object_manifest.gml"));
        Assert.That(point.Files,
            Is.EqualTo(new[] { "assets/gml/NpcId.gml", "assets/gml/object_manifest.gml" }));
    }

    [Test]
    public void ShouldFoldVacancyTemplatesOntoTheirSites()
    {
        var point = Load(Base + GoodPoint).Extensions.Single();

        Assert.That(point.Sites[0].VacancyTemplate, Is.EqualTo("{{symbol}} = {{ordinal}},"));
        Assert.That(point.Sites[2].VacancyTemplate, Is.Empty);
    }

    [Test]
    public void ShouldParseVacancyFiles()
    {
        var file = Load(Base + GoodPoint).VacancyFiles();

        Assert.That(file.Path, Is.EqualTo("fiddle/npcs/{{symbol}}.toml"));
        Assert.That(file.Content, Does.Contain("Departed Villager"));
    }

    [Test]
    public void ShouldRequireAnOrdinalDomainAndSayWhy()
    {
        var text = Base + GoodPoint.Replace("[extension.ordinal]\nenum     = \"NpcId\"\nsentinel = \"LEN\"\n", "");

        var errors = ErrorsFor(text);

        Assert.That(errors, Does.Contain("missing `[extension.ordinal]`"));
        // the message must say the restriction is deliberate, so the next
        // person does not read it as an oversight and "fix" it
        Assert.That(errors, Does.Contain("deliberate restriction"));
    }

    [Test]
    public void ShouldShareTheIdNamespaceWithSeamsAndFixes()
    {
        var collide = "\n" + """
            [[engine_fix]]
            name    = "roster"
            file    = "gml/A.gml"
            anchor  = "a();"
            replace = "b(); // t_x"
            marker  = "t_x"
            """ + "\n";

        Assert.That(ErrorsFor(Base + GoodPoint + collide), Does.Contain("duplicate id/name 'roster'"));
    }

    [Test]
    public void ShouldRequireExactlyOneEnumMemberSite()
    {
        var errors = ErrorsFor(With("id       = \"id_to_obj\"\nkind     = \"anchor\"",
            "id       = \"id_to_obj\"\nkind     = \"enum_member\""));

        Assert.That(errors, Does.Contain("declares 2 `enum_member` site(s)"));
    }

    [Test]
    public void ShouldRejectAnEnumMemberSiteOverridingItsFile()
    {
        var errors = ErrorsFor(With(
            "id       = \"enum_member\"\nkind     = \"enum_member\"",
            "id       = \"enum_member\"\nkind     = \"enum_member\"\nfile     = \"gml/Other.gml\""));

        Assert.That(errors, Does.Contain("cannot: the ordinal enum lives in the point's own file"));
    }

    [Test]
    public void ShouldRejectDuplicateSiteIds()
    {
        var errors = ErrorsFor(With("id       = \"object_macro\"", "id       = \"id_to_obj\""));

        Assert.That(errors, Does.Contain("declares site 'id_to_obj' twice"));
    }

    [Test]
    public void ShouldRejectAnAnchorSiteMissingItsAnchorOrPlace()
    {
        Assert.That(ErrorsFor(With("anchor   = \"        default: return undefined;\"\n", "")),
            Does.Contain("is an anchor site and needs `anchor`"));
        Assert.That(ErrorsFor(With("place    = \"before\"", "place    = \"sideways\"")),
            Does.Contain("place 'sideways' is not before or after"));
    }

    [Test]
    public void ShouldRejectAnchorOrPlaceOnANonAnchorSite()
    {
        var errors = ErrorsFor(With(
            "kind     = \"append\"\nfile     = \"gml/object_manifest.gml\"",
            "kind     = \"append\"\nplace    = \"before\"\nfile     = \"gml/object_manifest.gml\""));

        Assert.That(errors, Does.Contain("is a append site - drop `place`"));
    }

    [Test]
    public void ShouldRejectAnUnknownPlaceholderInATemplate()
    {
        var errors = ErrorsFor(With("return {{object}};", "return {{objct}};"));

        Assert.That(errors, Does.Contain("unknown placeholder(s) {{objct}}"));
        Assert.That(errors, Does.Contain("declared fields (object)"));
    }

    [Test]
    public void ShouldRejectAFieldPlaceholderInAVacancyTemplate()
    {
        // a vacancy has no mod behind it, so the mod's field values are gone
        var errors = ErrorsFor(With(
            "id_to_obj    = \"case NpcId.{{symbol}}: return obj_vacant;\"",
            "id_to_obj    = \"case NpcId.{{symbol}}: return {{object}};\""));

        Assert.That(errors, Does.Contain("vacancy template references unknown placeholder(s) {{object}}"));
    }

    [Test]
    public void ShouldRejectAnUnknownVacancyKey()
    {
        // the typo case. Silently dropping the enum_member vacancy line is a
        // gap ordinal, which crashes the game at launch
        var errors = ErrorsFor(With("id_to_obj    = \"case NpcId", "id_to_object = \"case NpcId"));

        Assert.That(errors, Does.Contain("key 'id_to_object' names no declared site"));
        Assert.That(errors, Does.Contain("crashes the game at launch"));
    }

    [Test]
    public void ShouldRequireAVacancyEntryForEverySite()
    {
        var errors = ErrorsFor(With("object_macro = \"\"\n", ""));

        Assert.That(errors, Does.Contain("site 'object_macro' has no `[extension.vacancy]` entry"));
    }

    [Test]
    public void ShouldRejectAMultiLineTemplate()
    {
        var errors = ErrorsFor(With(
            "template = \"case NpcId.{{symbol}}: return {{object}};\"",
            "template = '''\ncase NpcId.{{symbol}}:\n    return {{object}};'''"));

        Assert.That(errors, Does.Contain("template spans lines"));
    }

    [Test]
    public void ShouldRejectABadFieldNameOrType()
    {
        Assert.That(ErrorsFor(With("name = \"object\"", "name = \"Object\"")),
            Does.Contain("is not lower_snake_case"));
        Assert.That(ErrorsFor(With("type = \"identifier\"", "type = \"gml\"")),
            Does.Contain("type 'gml' is not one of identifier, string, int, bool"));
    }

    [Test]
    public void ShouldRejectAnUnsafeVacancyFilePath()
    {
        var errors = ErrorsFor(With(
            "path    = \"fiddle/npcs/{{symbol}}.toml\"",
            "path    = \"fiddle/npcs/up/{{symbol}}.toml\"".Replace("up", "..")));

        Assert.That(errors, Does.Contain("carries a '.' or '..' segment"));
    }

    [Test]
    public void ShouldRejectAVacancyFileUnderTheGmlTree()
    {
        // a vacancy stub is data. Under assets/gml/ it would reach the compile
        // gate's collection and be handed to momi-gml-check as if it were source
        var errors = ErrorsFor(With(
            "path    = \"fiddle/npcs/{{symbol}}.toml\"",
            "path    = \"gml/scripts/mmapi/{{symbol}}.toml\""));

        Assert.That(errors, Does.Contain("is under assets/gml/"));
        Assert.That(errors, Does.Contain("data, not source"));
    }

    [Test]
    public void ShouldRejectTwoVacancyFilesSharingAPathTemplate()
    {
        var second = "\n" + """
            [[extension.vacancy_files]]
            path    = "fiddle/npcs/{{symbol}}.toml"
            content = "name = \"Other\"\n"
            """ + "\n";

        Assert.That(ErrorsFor(Base + GoodPoint + second),
            Does.Contain("declares two `vacancy_files` with path template"));
    }

    [Test]
    public void ShouldRejectAnUnknownPlaceholderInAVacancyFile()
    {
        var errors = ErrorsFor(With("path    = \"fiddle/npcs/{{symbol}}.toml\"",
            "path    = \"fiddle/npcs/{{object}}.toml\""));

        Assert.That(errors, Does.Contain("path references unknown placeholder(s) {{object}}"));
    }

    [Test]
    public void ShouldParseCompanions()
    {
        var companion = Load(Base + GoodPoint).Extensions.Single().Companions.Single();

        Assert.That(companion.Path, Is.EqualTo("fiddle/npcs/{{symbol}}.toml"));
        Assert.That(companion.Level, Is.EqualTo(ExtensionCompanionLevel.Error));
        Assert.That(companion.Doc, Does.Contain("crashes during Setup"));
    }

    [Test]
    public void ShouldRejectABadCompanionLevel()
    {
        var errors = ErrorsFor(With("level = \"error\"", "level = \"fatal\""));

        Assert.That(errors, Does.Contain("level 'fatal' is not one of error, warning"));
    }

    [Test]
    public void ShouldRequireACompanionDocBecauseItIsTheMessage()
    {
        var errors = ErrorsFor(With(
            "doc   = \"The NPC prototype. Absent, the game crashes during Setup.\"\n", ""));

        Assert.That(errors, Does.Contain("is missing `doc`"));
        Assert.That(errors, Does.Contain("does not say why the file matters"));
    }

    [Test]
    public void ShouldRejectAnUnsafeCompanionPath()
    {
        var errors = ErrorsFor(With(
            "path  = \"fiddle/npcs/{{symbol}}.toml\"",
            "path  = \"fiddle/up/{{symbol}}.toml\"".Replace("up", "..")));

        Assert.That(errors, Does.Contain("carries a '.' or '..' segment"));
    }

    [Test]
    public void ShouldRejectAnUnknownPlaceholderInACompanionPath()
    {
        var errors = ErrorsFor(With(
            "path  = \"fiddle/npcs/{{symbol}}.toml\"",
            "path  = \"fiddle/npcs/{{object}}.toml\""));

        Assert.That(errors, Does.Contain("path references unknown placeholder(s) {{object}}"));
    }

    [Test]
    public void ShouldRejectTwoCompanionsSharingAPathTemplate()
    {
        var second = "\n" + """
            [[extension.companions]]
            path  = "fiddle/npcs/{{symbol}}.toml"
            level = "warning"
            doc   = "Also this."
            """ + "\n";

        Assert.That(ErrorsFor(Base + GoodPoint + second),
            Does.Contain("declares two `companions` with path template"));
    }

    [Test]
    public void ShouldRequireVacancyFilesWhenAnErrorCompanionIsDeclared()
    {
        // an error-level companion with no vacancy_files would let the
        // machinery itself render an enum member with no data, the crash state
        var text = Base + GoodPoint
            .Replace("[[extension.vacancy_files]]\n", "")
            .Replace("path    = \"fiddle/npcs/{{symbol}}.toml\"\ncontent = \"name = \\\"Departed Villager\\\"\\n\"\n", "");

        var errors = ErrorsFor(text);

        Assert.That(errors, Does.Contain("declares an error-level companion but no"));
        Assert.That(errors, Does.Contain("every excluded registrant bricks the game"));
    }

    [Test]
    public void ShouldParseAnAppendSiteCommentAndDefaultTheRest()
    {
        var point = Load(Base + GoodPoint.Replace(
            "kind     = \"append\"",
            "kind     = \"append\"\ncomment  = \"#\"")).Extensions.Single();

        Assert.That(point.Sites.Single(s => s.Id == "object_macro").Comment, Is.EqualTo("#"));
        Assert.That(point.Sites.Single(s => s.Id == "enum_member").Comment, Is.EqualTo("//"));
    }

    [Test]
    public void ShouldRejectAHashCommentOnANonAppendSite()
    {
        var errors = ErrorsFor(With(
            "id       = \"id_to_obj\"\nkind     = \"anchor\"",
            "id       = \"id_to_obj\"\nkind     = \"anchor\"\ncomment  = \"#\""));

        Assert.That(errors, Does.Contain("only append sites may target non-GML files"));
    }

    [Test]
    public void ShouldRejectAnUnknownCommentLeader()
    {
        var errors = ErrorsFor(With(
            "kind     = \"append\"",
            "kind     = \"append\"\ncomment  = \";\""));

        Assert.That(errors, Does.Contain("comment ';' is not // or #"));
    }

    [Test]
    public void ShouldAcceptAPointWithNoCompanions()
    {
        var text = Base + GoodPoint[..GoodPoint.IndexOf("[[extension.companions]]", StringComparison.Ordinal)];

        Assert.That(Load(text).Extensions.Single().Companions, Is.Empty);
    }

    [Test]
    public void ShouldBatchEveryProblemRatherThanStoppingAtTheFirst()
    {
        var broken = With("place    = \"before\"", "place    = \"sideways\"")
            .Replace("type = \"identifier\"", "type = \"gml\"");

        var errors = ErrorsFor(broken);

        Assert.That(errors, Does.Contain("sideways"));
        Assert.That(errors, Does.Contain("'gml'"));
    }

    [Test]
    public void ShouldShipTheNpcRosterAndStatusEffectPoints()
    {
        // The catalog ships npc_roster and status_effect. The second is
        // deliberately the simple shape (enum member only,
        // vacancy-benign, no companions, no vacancy files). This pins both
        // so a catalog edit that drops a site or companion fails here, not
        // in someone's install.
        var (name, bytes) = PayloadResolver.SeamCatalog();

        var extensions = SeamCatalogLoader.Load(bytes, name).Extensions;
        Assert.That(extensions.Select(e => e.Id), Is.EqualTo(new[] { "npc_roster", "status_effect" }));

        var status = extensions.Single(e => e.Id == "status_effect");
        Assert.That(status.OrdinalEnum, Is.EqualTo("StatusEffectId"));
        Assert.That(status.Sites.Single().Kind, Is.EqualTo(ExtensionSiteKind.EnumMember));
        Assert.That(status.Companions, Is.Empty);
        Assert.That(status.VacancyFiles, Is.Empty);

        var point = extensions.Single(e => e.Id == "npc_roster");

        Assert.That(point.Id, Is.EqualTo("npc_roster"));
        Assert.That(point.OrdinalEnum, Is.EqualTo("NpcId"));
        Assert.That(point.Sites.Select(s => s.Id), Is.EqualTo(new[]
        {
            "enum_member", "id_to_obj", "obj_to_id", "object_macro", "basement_schedule",
        }));
        // The schedule site targets TOML, so its marker leader is #
        Assert.That(point.Sites.Single(s => s.Id == "basement_schedule").Comment, Is.EqualTo("#"));
        Assert.That(point.Sites.Where(s => s.Id != "basement_schedule"),
            Has.All.Matches<ExtensionSite>(s => s.Comment == "//"));
        // The data companion is error-level, and vacancies get a stub
        Assert.That(point.Companions.Single().Level, Is.EqualTo(ExtensionCompanionLevel.Error));
        Assert.That(point.VacancyFiles.Single().Path, Is.EqualTo("fiddle/npcs/{{symbol}}.toml"));
        // The stub must not declare portraits with content, because their sprite
        // names derive from the symbol through a fatal lookup
        Assert.That(point.VacancyFiles.Single().Content, Does.Contain("portraits = {}"));
    }
}

internal static class ExtensionTestExtensions
{
    public static ExtensionVacancyFile VacancyFiles(this SeamCatalog catalog) =>
        catalog.Extensions.Single().VacancyFiles.Single();
}
