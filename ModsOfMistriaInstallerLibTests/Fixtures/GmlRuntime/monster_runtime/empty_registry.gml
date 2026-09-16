global.monster_native_load_called = false;
var clean = [0, 0, 0];
mmapi_monster_load_kills(clean, { mushroom: 2, bat: 3 });
dcheck("a clean save keeps the native load path", global.monster_native_load_called);
deq("the native path restores a pristine count", clean[MonsterId.Bat], 3);

global.monster_native_load_called = false;
var departed = [0, 0, 0];
mmapi_monster_load_kills(departed, { mushroom: 5, departed_monster: 8 });
dcheck("an unavailable saved name selects tolerant loading", !global.monster_native_load_called);
deq("tolerant loading still restores available names", departed[MonsterId.Mushroom], 5);
