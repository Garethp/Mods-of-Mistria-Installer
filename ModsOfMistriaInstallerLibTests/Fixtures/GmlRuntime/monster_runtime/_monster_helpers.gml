enum MonsterId {
    Mushroom,
    Bat,
    ExampleEnemyRoller,
    LEN
}

function monster_id_to_string(monster_id) {
    switch monster_id {
        case MonsterId.Mushroom: return "mushroom";
        case MonsterId.Bat: return "bat";
        case MonsterId.ExampleEnemyRoller: return "example_enemy_roller";
    }
    return undefined;
}

function try_string_to_monster_id(name) {
    switch name {
        case "mushroom": return MonsterId.Mushroom;
        case "bat": return MonsterId.Bat;
        case "example_enemy_roller": return MonsterId.ExampleEnemyRoller;
    }
    return undefined;
}

function array_to_struct(input, functor) {
    var output = {};
    for (var i = 0; i < array_length(input); i++)
        output[$ functor(i)] = input[i];
    return output;
}

function apply_struct_to_array(input, saved, functor) {
    global.monster_native_load_called = true;
    var names = struct_get_names(saved);
    for (var i = 0; i < array_length(names); i++)
        input[functor(names[i])] = saved[$ names[i]];
}
