// instance.created registrations carry their scope and are refused without one.
//
// The poll (mmapi_instances_poll) walks each registration's object with `with`,
// which needs live instances the probe VM does not have, so this fixture proves
// the registration half only: the refusal, the object on the record, the marker
// per registration, and the duplicate rule that includes the object. The object
// values here are plain numbers standing in for object assets, which the
// registry never inspects.

function ic_a(inst) { }
function ic_b(inst) { }

mmapi_on("instance.created", ic_a);
dcheck("an unscoped registration is refused", global.__mmapi_hooks[$ "instance.created"] == undefined);

mmapi_on("instance.created", ic_a, { object: 7001 });
var records = global.__mmapi_hooks[$ "instance.created"];
deq("a scoped registration lands", array_length(records), 1);
deq("the record carries its object", records[0].object, 7001);
dcheck("the record carries a marker key", string_pos("__mmapi_seen_", records[0].marker) == 1);

mmapi_on("instance.created", ic_a, { object: 7002 });
deq("the same handler can watch a second object", array_length(global.__mmapi_hooks[$ "instance.created"]), 2);

mmapi_on("instance.created", ic_a, { object: 7002 });
deq("the same handler on the same object lands once", array_length(global.__mmapi_hooks[$ "instance.created"]), 2);

mmapi_on("instance.created", ic_b, { object: 7001 });
var two = global.__mmapi_hooks[$ "instance.created"];
deq("three registrations in all", array_length(two), 3);
dcheck("markers are unique per registration", two[0].marker != two[1].marker && two[1].marker != two[2].marker && two[0].marker != two[2].marker);

mmapi_on("game.new_day", ic_a);
deq("other hooks carry no scope", global.__mmapi_hooks[$ "game.new_day"][0].object, undefined);
