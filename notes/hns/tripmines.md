## HNS Tripmine Notes

* Visibility of tripmine laser would be lowered (more transparent, barely visible) for non owning teams

### Tripmine Detection Sequence:

* detect players on enemy team
* Play sound: `HACKING_PUZZLE_LOCK_ALARM`
* Place Navmarker (only visible for owning team?):
    - On Mine?
    - On the detected player?
        - Follow player for a short period (similar to pablo biotracker ping?; maybe longer)
        - stationary at player pos

### Team Switch Behaviour (H->S)

* On team switch:
    - Detonate mine? (Could kill other hiders potentially?)
    - Deactivate mine (despawn without any explosion)
    - Keep mine, but works for other team now
        * Reactivation-cooldown
        * laser disabled, light flashing, beeping noises
        * reactivate after cooldown for new team

### Tripmine counterplay

* Players could jump the beam / crouch under
    * -> using up more resources to place 2 mines stacked = balanced?

* Should players (Hiders) be able to shoot down mines?
    - deincentivize doing so by punishing the shooter with a navmarker ping on themself?
    - make mines hackable?
        * On hack fail
            - explode :)
            - navmarker ping + hacking cooldown
        * On hack success
            * mine disabled for a few seconds
            * seekers notified after that a hack occured?