var native_id = MonsterId.Mushroom;
var custom_id = MonsterId.ExampleEnemyRoller;

dcheck("a pristine MonsterId remains vanilla", mmapi_monster_is_vanilla_id(native_id));
dcheck("a generated MonsterId is custom", mmapi_monster_is_custom_id(custom_id));
deq("the generated registry reports the owning mod", mmapi_monster_owner(custom_id), "example.enemy_roller");
dcheck("a vanilla MonsterId has no mod owner", mmapi_monster_owner(native_id) == undefined);

var saved = mmapi_monster_save_kills([3, 4, 5]);
deq("a pristine kill count is saved", saved.mushroom, 3);
deq("a second pristine kill count is saved", saved.bat, 4);
dcheck("a custom kill count is omitted", saved[$ "example_enemy_roller"] == undefined);

global.monster_native_load_called = false;
var loaded = [0, 0, 0];
mmapi_monster_load_kills(loaded, {
    mushroom: 7,
    example_enemy_roller: 9,
    departed_monster: 11,
});
deq("a pristine kill count loads", loaded[MonsterId.Mushroom], 7);
deq("the current custom kill count is not restored", loaded[MonsterId.ExampleEnemyRoller], 0);
dcheck("custom loading uses the exact pristine allowlist", !global.monster_native_load_called);
