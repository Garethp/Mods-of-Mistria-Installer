function AudioDemo_playChime() {
    if (TANGO.name_exists("AudioDemo/HotkeyChime")) {
        tango_play("AudioDemo/HotkeyChime", undefined, undefined, 1.0);
        mmapi_log_info("audiodemo", "Played AudioDemo/HotkeyChime.");
    } else {
        mmapi_log_info("audiodemo", "AudioDemo/HotkeyChime not registered is the mod correctly installed?");
    }
    mmapi_log_flush("audiodemo");
}

function AudioDemo_setup(_ctx) {
    if (global[$ "__audiodemo_setup"] == true) { return; }
    global.__audiodemo_setup = true;

    var _vk = mmapi_hotkey_vk_from_name("F9");
    if (_vk != undefined) { mmapi_hotkey_register(_vk, AudioDemo_playChime); }

    mmapi_log_info("audiodemo", "Ready: Press F9 to play the new sound.");
    mmapi_log_flush("audiodemo");
}

mmapi_mod_declare("audiodemo", "1.0.0");
mmapi_on("game.title_entered", AudioDemo_setup);
