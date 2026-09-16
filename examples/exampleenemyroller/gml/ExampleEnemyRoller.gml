enum ExampleEnemyRollerState {
    Windup,
    Walk,
    Attack,
    Hurt,
    Dying,
    Bounce,
    Stunned,
    LEN
}

mmapi_mod_declare("example.enemy_roller", "0.1.0");

function example_enemy_roller_id() {
    return try_string_to_monster_id("example_enemy_roller");
}

function example_enemy_roller_log_state(_monster, _name) {
    mmapi_log_debug("example.enemy_roller", "state " + _name
        + " id=" + string(_monster.id) + " hp=" + string(_monster.hit_points));
}

function example_enemy_roller_log_sprite(_monster, _stage, _expected) {
    var _actual = _monster.sprite_index;
    if (_expected == undefined || _expected < 0 || _actual != _expected) {
        mmapi_log_warn("example.enemy_roller", "sprite " + _stage
            + " id=" + string(_monster.id)
            + " actual=" + string(_actual)
            + " fiddle=" + string(_expected));
        return;
    }
    mmapi_log_debug("example.enemy_roller", "sprite " + _stage
        + " id=" + string(_monster.id)
        + " sprite=" + string(_actual)
        + " size=" + string(sprite_get_width(_actual))
        + "x" + string(sprite_get_height(_actual))
        + " alpha=" + string(_monster.image_alpha)
        + " scale=" + string(_monster.image_xscale));
}

function example_enemy_roller_bounce(_monster, _collision=MovementCollisionDirection.NONE) {
    var _x = lengthdir_x(_monster.config.bounce_speed, _monster.dir);
    var _y = lengthdir_y(_monster.config.bounce_speed, _monster.dir);
    if (_collision == MovementCollisionDirection.NONE) {
        _x = -_x;
        _y = -_y;
    } else {
        if (has_flag(_collision, MovementCollisionDirection.HORIZONTAL)) _x = -_x;
        if (has_flag(_collision, MovementCollisionDirection.VERTICAL)) _y = -_y;
    }
    _monster.bounce_x = _x;
    _monster.bounce_y = _y;
    _monster.dir = point_direction(0, 0, _x, _y);
    _monster.fsm.change_state_instant(ExampleEnemyRollerState.Bounce);
    mmapi_log_debug("example.enemy_roller", "bounce id=" + string(_monster.id)
        + " collision=" + string(_collision));
}

function example_enemy_roller_die(_monster, _reason) {
    if (_monster.fsm.current_state_id() == ExampleEnemyRollerState.Dying) return;
    _monster.receiver.drop_damage();
    _monster.fsm.change_state_instant(ExampleEnemyRollerState.Dying);
    mmapi_log_info("example.enemy_roller", "death id=" + string(_monster.id)
        + " reason=" + _reason
        + " damage_taken=" + string(_monster.stats_entry.damage_taken));
    monster_death_poof(_monster);
}

function example_enemy_roller_spawn() {
    if (!instance_exists(obj_ari)) {
        mmapi_log_warn("example.enemy_roller", "spawn ignored: no player instance exists");
        return;
    }
    var _id = example_enemy_roller_id();
    if (_id == undefined) {
        mmapi_log_warn("example.enemy_roller", "spawn ignored: monster id unavailable");
        return;
    }
    var _roller = spawn_monster(obj_ari.x + 48, obj_ari.y, _id);
    mmapi_log_info("example.enemy_roller", "spawned Roller id=" + string(_id)
        + " instance=" + string(_roller));
    return _roller;
}

function example_enemy_roller_install() {
    if (global[$ "example_enemy_roller_installed"] == true) return;
    var _f5 = mmapi_hotkey_vk_from_name("F5");
    if (_f5 == undefined) {
        mmapi_log_warn("example.enemy_roller", "spawn hotkey unavailable: F5 did not resolve");
        return;
    }
    mmapi_hotkey_register(_f5, example_enemy_roller_spawn, { mod_name: "example.enemy_roller" });
    global.example_enemy_roller_installed = true;
    mmapi_log_info("example.enemy_roller", "spawn hotkey ready (vk " + string(_f5) + ")");
}

example_enemy_roller_install();

object_create("obj_example_enemy_roller", object_reserve("par_monster"), {
    // par_monster sets sprite_index and creates the damage receiver.
    sprite_index: undefined,
    create: function() {
        event_inherit(ObjectEvent.Create);
        example_enemy_roller_log_sprite(self, "after parent Create",
            config.sprite_catalogue[ExampleEnemyRollerState.Windup][Cardinal.South]);
        self.charge_tarball = undefined;
        self.bounce_x = 0;
        self.bounce_y = 0;
        self.image_alpha = 1;
        self.image_xscale = self.config.draw_scale;
        self.image_yscale = self.config.draw_scale;
        self.image_speed = 1;

        var _body_shape = undefined;
        try { _body_shape = animation_to_shape(self.sprite_index); }
        catch (_shape_error) {
            mmapi_log_warn("example.enemy_roller", "body shape lookup failed: " + string(_shape_error));
        }
        if (_body_shape != undefined) {
            self.mask_index = _body_shape;
            self.receiver.mask_index = _body_shape;
            mmapi_log_debug("example.enemy_roller", "body shape ready id=" + string(id)
                + " shape=" + string(_body_shape));
        } else {
            mmapi_log_warn("example.enemy_roller",
                "body shape unavailable; using the animation bounds");
        }

        self.on_hit = function(_receiver=undefined) {
            if (self.fsm.current_state_id() == ExampleEnemyRollerState.Attack) {
                self.fsm.current_state().player_hit = true;
                mmapi_log_debug("example.enemy_roller", "charge hit player id=" + string(self.id));
            }
        };

        // Sprite states occupy the first category-defined slots.
        fsm = StateMachineBuilder(ExampleEnemyRollerState.LEN)
            .add_state(StateBuilder(ExampleEnemyRollerState.Windup)
                .start(function() {
                    owner.set_state_sprite(true, ExampleEnemyRollerState.Windup);
                    example_enemy_roller_log_state(owner, "windup");
                })
                .step(function() {
                    owner.move.x = 0;
                    owner.move.y = 0;
                    if (fsm.state_frame < owner.config.windup_frames) return;
                    if (!instance_exists(obj_ari)) {
                        fsm.change_state(ExampleEnemyRollerState.Walk);
                        return;
                    }
                    owner.dir = point_direction(owner.x, owner.y, obj_ari.x, obj_ari.y);
                    fsm.change_state(ExampleEnemyRollerState.Attack);
                }).spawn())
            .add_state(StateBuilder(ExampleEnemyRollerState.Walk)
                .start(function() {
                    owner.dir = irandom_range(0, 359);
                    owner.set_state_sprite(true, ExampleEnemyRollerState.Walk);
                    example_enemy_roller_log_state(owner, "walk");
                })
                .step(function() {
                    owner.move.x = lengthdir_x(owner.config.speed, owner.dir);
                    owner.move.y = lengthdir_y(owner.config.speed, owner.dir);
                    if (instance_exists(obj_ari)
                        && point_distance(owner.x, owner.y, obj_ari.x, obj_ari.y)
                            <= owner.config.attack_radius) {
                        owner.move.x = 0;
                        owner.move.y = 0;
                        fsm.change_state(ExampleEnemyRollerState.Windup);
                    }
                }).spawn())
            .add_state(StateBuilder(ExampleEnemyRollerState.Attack)
                .start(function() {
                    owner.can_overlap_ari = true;
                    owner.set_state_sprite(true, ExampleEnemyRollerState.Attack);
                    player_hit = false;
                    owner.charge_tarball = TarballBuilder(
                        owner.x, owner.y,
                        owner.config.charge_width, owner.config.charge_height,
                        owner.config.damage)
                        .set_offset(owner.config.charge_offset_x,
                            owner.config.charge_offset_y)
                        .set_can_destroy_grid_objects(false)
                        .set_parent(owner)
                        .set_provenance(undefined, owner.stats_entry)
                        .notify(owner)
                        .set_persists()
                        .gen();
                    example_enemy_roller_log_state(owner, "attack");
                    mmapi_log_debug("example.enemy_roller", "charge active id=" + string(owner.id)
                        + " dir=" + string(owner.dir)
                        + " damage=" + string(owner.config.damage));
                })
                .step(function() {
                    if (self.player_hit) {
                        owner.move.x = 0;
                        owner.move.y = 0;
                        example_enemy_roller_bounce(owner);
                        return;
                    }
                    if (fsm.state_frame >= owner.config.charge_frames) {
                        owner.move.x = 0;
                        owner.move.y = 0;
                        mmapi_log_debug("example.enemy_roller", "charge timed out id=" + string(owner.id));
                        example_enemy_roller_bounce(owner);
                        return;
                    }
                    owner.move.x = lengthdir_x(owner.config.charge_speed, owner.dir);
                    owner.move.y = lengthdir_y(owner.config.charge_speed, owner.dir);
                })
                .stop(function() {
                    instance_destroy_safe(owner.charge_tarball);
                    owner.charge_tarball = undefined;
                    owner.can_overlap_ari = false;
                }).spawn())
            .add_state(StateBuilder(ExampleEnemyRollerState.Hurt)
                .start(function() {
                    owner.z = 0;
                    owner.set_state_sprite(true, ExampleEnemyRollerState.Hurt);
                    example_enemy_roller_log_state(owner, "hurt");
                })
                .step(function() {
                    owner.move.x = 0;
                    owner.move.y = 0;
                    if (fsm.state_frame >= owner.config.hurt_frames)
                        fsm.change_state(ExampleEnemyRollerState.Walk);
                }).spawn())
            .add_state(StateBuilder(ExampleEnemyRollerState.Dying)
                .start(function() {
                    owner.move.x = 0;
                    owner.move.y = 0;
                    owner.z = 0;
                    owner.can_overlap_ari = false;
                    owner.set_state_sprite(true, ExampleEnemyRollerState.Dying);
                    example_enemy_roller_log_state(owner, "dying");
                }).spawn())
            .add_state(StateBuilder(ExampleEnemyRollerState.Bounce)
                .start(function() {
                    owner.can_overlap_ari = true;
                    owner.z = 0;
                    jump_velocity = owner.config.bounce_jump_velocity;
                    owner.set_state_sprite(true, ExampleEnemyRollerState.Windup);
                    example_enemy_roller_log_state(owner, "bounce");
                })
                .step(function() {
                    owner.move.x = owner.bounce_x;
                    owner.move.y = owner.bounce_y;
                    owner.z += self.jump_velocity;
                    self.jump_velocity += owner.config.bounce_gravity;
                    if (owner.z >= 0) {
                        owner.z = 0;
                        owner.move.x = 0;
                        owner.move.y = 0;
                        fsm.change_state(ExampleEnemyRollerState.Stunned);
                    }
                })
                .stop(function() { owner.can_overlap_ari = false; }).spawn())
            .add_state(StateBuilder(ExampleEnemyRollerState.Stunned)
                .start(function() {
                    owner.set_state_sprite(true, ExampleEnemyRollerState.Windup);
                    example_enemy_roller_log_state(owner, "stunned");
                })
                .step(function() {
                    owner.move.x = 0;
                    owner.move.y = 0;
                    if (fsm.state_frame >= owner.config.stun_frames)
                        fsm.change_state(ExampleEnemyRollerState.Walk);
                }).spawn())
            .spawn(ExampleEnemyRollerState.Walk, self, Map());

        self.depth = get_instance_depth(y, z);
        example_enemy_roller_log_sprite(self, "in initial Walk",
            config.sprite_catalogue[ExampleEnemyRollerState.Walk][Cardinal.South]);
        mmapi_log_debug("example.enemy_roller", "created id=" + string(id)
            + " monster_id=" + string(monster_id)
            + " sprite=" + string(sprite_index)
            + " hp=" + string(hit_points));
    },
    step: function() {
        event_inherit(ObjectEvent.Step);
        if (game_paused()) return;
        fsm.step();
        if (!instance_exists(self)) return;
        var _collision = movement_and_collide();
        if (_collision != MovementCollisionDirection.NONE) {
            if (fsm.current_state_id() == ExampleEnemyRollerState.Attack)
                example_enemy_roller_bounce(self, _collision);
            else if (fsm.current_state_id() == ExampleEnemyRollerState.Walk)
                self.dir = irandom_range(0, 359);
        }
        self.depth = get_instance_depth(y, z);
    },
    step_end: function() {
        if (game_paused()) return;
        event_inherit(ObjectEvent.StepEnd);
        if (fsm.current_state_id() == ExampleEnemyRollerState.Dying) return;
        self.process_status();
        fsm.end_step();
        if (!instance_exists(self)) return;
        if (self.hit_points <= 0) {
            example_enemy_roller_die(self, "status");
            return;
        }
        while (true) {
            var _received = self.receiver.try_take_damage();
            if (_received == undefined) break;
            if (_received.status != ReceiverStatus.Normal) continue;
            var _damage = max(0, _received.tarball.damage);
            if (_damage <= 0) {
                if ((_received.tarball.knockback ?? false)
                    && _received.tarball.parent_object_id == obj_ari
                    && fsm.current_state_id() == ExampleEnemyRollerState.Attack) {
                    mmapi_log_debug("example.enemy_roller", "guard repelled charge id=" + string(id));
                    example_enemy_roller_bounce(self);
                }
                continue;
            }
            self.process_tarball_status(_received.tarball);
            self.hit_points -= _damage;
            self.stats_entry.damage_taken += _damage;
            self.stats_entry.damage_taken_count += 1;
            self.patience.value = PATIENCE_DAMAGED;
            self.show_damage = 30;
            spawn_damage_numbers(self, self.config.damage_number_offset,
                _damage, _received.tarball.damage_flag());
            TANGO.play(self.config.misc_tango.strong_damage, x, y);
            if (_received.tarball.critical) monster_critical_fx(self);
            mmapi_log_debug("example.enemy_roller", "damage id=" + string(id)
                + " amount=" + string(_damage) + " hp=" + string(hit_points)
                + " critical=" + string(_received.tarball.critical));
            if (self.hit_points <= 0) {
                example_enemy_roller_die(self, "receiver");
                break;
            }
            self.z = 0;
            fsm.change_state(ExampleEnemyRollerState.Hurt);
        }
    },
    destroy: function() {
        mmapi_log_debug("example.enemy_roller", "destroy id=" + string(id)
            + " hp=" + string(hit_points));
        event_inherit(ObjectEvent.Destroy);
    },
});
