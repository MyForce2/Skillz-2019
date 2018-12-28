from elf_kingdom import *


def do_turn(game):
    handle_elves(game)
    handle_portals(game)

def handle_elves(game):
    elves = sorted(game.get_my_living_elves(), key=lambda x: x.distance(game.get_enemy_castle()))
    if len(elves) == 1:
        defensive_elf(game, elves[0])
    elif len(elves) > 1:
        defensive_elf(game, elves[1])
        attack_elf(game, elves[0])

def defensive_elf(game, elf):
    final = game.get_enemy_creatures() + game.get_enemy_portals() + [game.get_enemy_castle()]
    to_attack = [game.get_enemy_living_elves(), game.get_enemy_portals(), final]
    for a in to_attack:
        if try_attack(elf, a):
            return
    elf.move_to(sorted(final, key=lambda x:elf.distance(x))[0])
    

def attack_elf(game, elf):
    if elf.distance(game.get_enemy_castle()) < 3000:
        if elf.can_build_portal():
            elf.build_portal()
            return
    if not elf.is_building:
        elf.move_to(game.get_enemy_castle())


def try_attack(elf, enemys):
    if len(enemys) > 0:
        for enemy in enemys:
            if elf.in_attack_range(enemy):
                elf.attack(enemy)
                return True
    return False
    

def handle_portals(game):
    portals = sorted(game.get_my_portals(), key=lambda x: x.distance(game.get_enemy_castle()))
    if len(portals) == 1:
        defensive_portal(game, portals[0])
    else:
        defensive_portal(game, portals[1])
        attack_portal(game, portals[0])
            
            
def attack_portal(game, portal):
    if portal.can_summon_lava_giant():
        portal.summon_lava_giant()

def defensive_portal(game, portal):
    enemys = game.get_enemy_living_elves() + game.get_enemy_lava_giants()
    my_def = game.get_my_ice_trolls()
    if len(my_def) < 10:
        if len(enemys) > 0:
            for enemy in enemys:
                if enemy.distance(game.get_my_castle()) < 3000:
                    if portal.can_summon_ice_troll():
                        portal.summon_ice_troll()