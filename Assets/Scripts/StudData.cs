using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using UnityEngine;

// Each stud is completely predefined and cannot be set from outside of the class,
// but after the instantiation said stud will be able to be changed (base stats
// can always be checked via the static instance internally)

// F:  1   -> 20
// D:  21  -> 40
// C:  41  -> 60
// B:  61  -> 80
// A:  81  -> 100
// S:  101 -> 120
// S+: 121 -> 150
public class Stud
{
    public StudType type;

    public int MaxHealth 
    {
        get => maxHealth;
    }
    private int maxHealth;

    public int MaxStamina
    {
        get => maxStamina;
    }
    private int maxStamina;

    public int shield;
    public int health;
    public int stamina;
    public int power;

    public List<StudMove> moves;

    public Dictionary<MoveEffect, int> effects;

    private Stud(StudType type, int health, int stamina, int power, List<StudMove> moves)
    {
        this.type     = type;

        maxHealth     = health;
        this.health   = health;

        maxStamina    = stamina;
        this.stamina  = stamina;

        this.power    = power;
        this.moves    = moves;

        shield = 0;

        effects = new Dictionary<MoveEffect, int>();
        effects.Clear();
    }

    // Start Turn
    public void StartTurn(StudMove move, bool client)
    {
        ApplyMove(move, client);
        ApplyEffects();
    }

    private void ApplyMove(StudMove move, bool client)
    { 
        if (shield > 0)
        {
            shield -= Mathf.Clamp(move.finalDmg - (stamina / 3), 0, 150);

            if (shield < 0)
            {
                health += shield;
                shield = 0;
            }
        }
        else
            health -= Mathf.Clamp(move.finalDmg - (stamina / 3), 0, maxHealth);

        health = Mathf.Clamp(health, 0, maxHealth);

        if (health <= 0)
        {
            GameSettings.Instance.Client.clientEvent.Invoke(client ? ClientState.DEAD : ClientState.WIN);

            if (client)
                GameSettings.Instance.Client.SendMove(StudMove.NoStamina);

            return;
        }

        if (move.effects == null) return;

        foreach (KeyValuePair<MoveEffect, int> effect in move.effects)
        {
            if (effect.Key == MoveEffect.AtkUp || effect.Key == MoveEffect.ExtraDmgHealth ||
                effect.Key == MoveEffect.Shield)
                continue;

            if (effects.ContainsKey(effect.Key))
                effects[effect.Key] += effect.Value;
            else
                effects.Add(effect.Key, effect.Value);
        }
    }

    private void ApplyEffects()
    {
        List<MoveEffect> effectsToRemove   = new List<MoveEffect>();
        List<MoveEffect> effectsToDecrease = new List<MoveEffect>();

        foreach (KeyValuePair<MoveEffect, int> effect in effects)
        {
            if (effect.Key == MoveEffect.Shield)
            {
                shield += effect.Value;

                Mathf.Clamp(shield, 0, 150);

                effectsToRemove.Add(effect.Key);
            }
            else
            {
                effectsToDecrease.Add(effect.Key);
            }
        }

        foreach (MoveEffect moveEffect in effectsToDecrease)
        {
            effects[moveEffect] -= 1;

            if (effects[moveEffect] <= 0)
                effectsToRemove.Add(moveEffect);
        }

        foreach (MoveEffect moveEffect in effectsToRemove)
            effects.Remove(moveEffect);

        effectsToRemove.Clear();
        effectsToDecrease.Clear();
    }

    // End Turn
    public StudMove PerformMove(StudMove moveBefore)
    {
        StudMove moveAfter = moveBefore;

        if (moveBefore.id == 17)
        {
            stamina += 10;
            stamina = Mathf.Clamp(stamina, 0, maxStamina);

            return moveAfter;
        }

        if (effects.ContainsKey(MoveEffect.Stun))
            return StudMove.Stun;

        if (effects.ContainsKey(MoveEffect.DoubleCost))
            moveAfter.cost *= 2;

        float dmgModifier = 1;

        dmgModifier += effects.ContainsKey(MoveEffect.AtkUp) ? 
            1.25f : 0;
        dmgModifier += effects.ContainsKey(MoveEffect.ExtraDmgHealth) ? 
            (150 - health) / 4 : 0;

        moveAfter.finalDmg = (moveAfter.baseDmg == 0) ? 
            0 : 
            (int) ((moveAfter.baseDmg + (power / 4)) * dmgModifier);

        stamina -= moveAfter.cost;

        if (stamina < 0)
        {
            Mathf.Clamp(stamina, 0, maxStamina);
            return StudMove.NoStamina;
        }

        Mathf.Clamp(stamina, 0, maxStamina);

        // Only add buffs after damage is calculated
        if (moveAfter.effects != null)
        {
            foreach (KeyValuePair<MoveEffect, int> effect in moveAfter.effects)
            {
                if (effect.Key == MoveEffect.DoubleCost || effect.Key == MoveEffect.Stun)
                    continue;

                if (effects.ContainsKey(effect.Key))
                    effects[effect.Key] += effect.Value;
                else
                    effects.Add(effect.Key, effect.Value);
            }
        }

        return moveAfter;
    }

    public static Stud GetStudByType(StudType studType)
    {
        switch (studType)
        {
            case StudType.PimpySonOpp:
                return PimpySonOpp;
            case StudType.ChumLee:
                return ChumLee;
            case StudType.Redrum:
                return Redrum;
            case StudType.MrMiagi:
                return MrMiagi;
            case StudType.Bape:
                return Bape;
            case StudType.Doughboy:
                return Doughboy;
            case StudType.ToleTole:
                return ToleTole;
            default:
                return PimpySonOpp;
            // rest
        }
    }

    // All studs available in the game
    private static Stud PimpySonOpp
    {
        get => new Stud(
            StudType.PimpySonOpp,
            90,
            60,
            60,
            new List<StudMove>() 
            {
                StudMove.Headbutt,
                StudMove.FerociousBite,
                StudMove.HeavyTackle,
                StudMove.Frown
            }
        );
    }
    private static Stud ChumLee
    {
        get => new Stud(
            StudType.ChumLee,
            135,
            35,
            85,
            new List<StudMove>()
            {
                StudMove.FerociousBite,
                StudMove.FiercePaw,
                StudMove.Frown,
                StudMove.Rest
            }
        );
    }
    private static Stud Redrum
    {
        get => new Stud(
            StudType.Redrum,
            40,
            80,
            140,
            new List<StudMove>()
            {
                StudMove.Headbutt,
                StudMove.ToadBlow,
                StudMove.Debauch,
                StudMove.Rage
            }
        );
    }
    private static Stud MrMiagi
    {
        get => new Stud(
            StudType.MrMiagi,
            60,
            105,
            95,
            new List<StudMove>()
            {
                StudMove.Headbutt,
                StudMove.BullyBreath,
                StudMove.Rest,
                StudMove.LastSpurt
            }
        );
    }
    private static Stud Bape
    {
        get => new Stud(
            StudType.Bape,
            150,
            65,
            25,
            new List<StudMove>()
            {
                StudMove.Headbutt,
                StudMove.Pressure,
                StudMove.Rest,
                StudMove.Rage
            }
        );
    }
    private static Stud Doughboy
    {
        get => new Stud(
            StudType.Doughboy,
            65,
            50,
            110,
            new List<StudMove>()
            {
                StudMove.Headbutt,
                StudMove.GreatestCoefficient,
                StudMove.ToTheEnd,
                StudMove.Rage,
            }
        );
    }
    private static Stud ToleTole
    {
        get => new Stud(
            StudType.ToleTole,
            255,
            150,
            150,
            new List<StudMove>()
            {
                StudMove.Headbutt,
                StudMove.FerociousBite,
                StudMove.TheEndOfAllThings,
                StudMove.Rage
            }
        );
    }
}

public struct StudMove
{
    public string name;
    public int    id;
    public int    cost;
    public int    finalDmg;
    public int    baseDmg;

    public Dictionary<MoveEffect, int> effects;

    private StudMove(string name, int id, int baseDmg, int cost, Dictionary<MoveEffect, int>? effects)
    {
        this.name = name;
        this.id = id;
        this.cost = cost;
        this.baseDmg = baseDmg;
        this.finalDmg = 0;

        this.effects = effects;
    }

    public static StudMove GetMoveById(int id)
    {
        switch(id)
        {
            default:
            case -1:
                return new StudMove();
            case 0:
                return FerociousBite;
            case 1:
                return Headbutt;
            case 2:
                return HeavyTackle;
            case 3:
                return FiercePaw;
            case 4:
                return ToadBlow;
            case 5:
                return BullyBreath;
            case 6:
                return GreatestCoefficient;
            case 7:
                return Frown;
            case 8:
                return Rest;
            case 9:
                return Debauch;
            case 10:
                return Rage;
            case 11:
                return LastSpurt;
            case 12:
                return Pressure;
            case 13:
                return ToTheEnd;
            case 14:
                return TheEndOfAllThings;
            case 15:
                return Stun;
            case 16:
                return NoStamina;
            case 17:
                return Skip;
        }
    }

    // Available moves in the game
    public static StudMove FerociousBite
    {
        get => new StudMove(
            name:    "Ferocious Bite",
            id:      0,
            baseDmg: 10,
            cost:    15,
            new Dictionary<MoveEffect, int> 
            { 
                { MoveEffect.Stun, 2 } 
            }
        );
    }
    public static StudMove Headbutt
    {
        get => new StudMove(
            name:    "Headbutt",
            id:      1,
            baseDmg: 10,
            cost:    5,
            null
        );
    }
    public static StudMove HeavyTackle
    {
        get => new StudMove(
            name:    "Heavy Tackle",
            id:      2,
            baseDmg: 35,
            cost:    25,
            null
        );
    }
    public static StudMove FiercePaw
    {
        get => new StudMove(
            name:    "Fierce Paw",
            id:      3,
            baseDmg: 5,
            cost:    10,
            new Dictionary<MoveEffect, int>
            {
                { MoveEffect.DoubleCost, 2 },
            }
        );
    }
    public static StudMove ToadBlow
    {
        get => new StudMove(
            name:    "Toad Blow",
            id:      4,
            baseDmg: 30,
            cost:    50,
            new Dictionary<MoveEffect, int>
            {
                { MoveEffect.DoubleCost, 2 },
            }
        );
    }
    public static StudMove BullyBreath
    {
        get => new StudMove(
            name:    "Bully Breath",
            id:      5,
            baseDmg: 25,
            cost:    55,
            new Dictionary<MoveEffect, int>
            {
                { MoveEffect.DoubleCost, 2 },
                { MoveEffect.Stun, 2 },
            }
        );
    }
    public static StudMove GreatestCoefficient
    {
        get => new StudMove(
            name:    "The Greatest Coefficient",
            id:      6,
            baseDmg: 55,
            cost:    50,
            null
        );
    }
    public static StudMove Frown
    {
        get => new StudMove(
            name:    "Frown",
            id:      7,
            baseDmg: 0,
            cost:    10,
            new Dictionary<MoveEffect, int>
            {
                { MoveEffect.Shield, 10 },
                { MoveEffect.AtkUp, 2 }
            }
        );
    }
    public static StudMove Rest
    {
        get => new StudMove(
            name:    "Rest",
            id:      8,
            baseDmg: 0,
            cost:    10,
            new Dictionary<MoveEffect, int>
            {
                { MoveEffect.Shield, 15 },
            }
        );
    }
    public static StudMove Debauch
    {
        get => new StudMove(
            name:    "Debauch",
            id:      9,
            baseDmg: 0,
            cost:    45,
            new Dictionary<MoveEffect, int>
            {
                { MoveEffect.Shield, 40 },
                { MoveEffect.AtkUp,  2 },
            }
        );
    }
    public static StudMove Rage
    {
        get => new StudMove(
            name:    "Rage",
            id:      10,
            baseDmg: 0,
            cost:    10,
            new Dictionary<MoveEffect, int>
            {
                { MoveEffect.AtkUp, 3 },
            }
        );
    }
    public static StudMove LastSpurt
    {
        get => new StudMove(
            name:   "Last Spurt",
            id:      11,
            baseDmg: 0,
            cost:    50,
            new Dictionary<MoveEffect, int>
            {
                { MoveEffect.Shield, 45 },
                { MoveEffect.ExtraDmgHealth, 3 }
            }
        );
    }
    public static StudMove Pressure
    {
        get => new StudMove(
            name:    "Pressure",
            id:      12,
            baseDmg: 15,
            cost:    50,
            new Dictionary<MoveEffect, int>
            {
                { MoveEffect.DoubleCost, 3 },
            }
        );
    }
    public static StudMove ToTheEnd
    {
        get => new StudMove(
            name:    "To the End",
            id:      13,
            baseDmg: 0,
            cost:    15,
            new Dictionary<MoveEffect, int>
            {
                { MoveEffect.ExtraDmgHealth, 5 },
            }
        );
    }
    public static StudMove TheEndOfAllThings
    {
        get => new StudMove(
            name:    "To the End",
            id:      14,
            baseDmg: 150,
            cost:    150,
            null
        );
    }
    public static StudMove Stun
    {
        get => new StudMove(
            name:    "Stunned!",
            id:      15,
            baseDmg: 0,
            cost:    0,
            null
        );
    }
    public static StudMove NoStamina
    {
        get => new StudMove(
            name: "No Stamina!",
            id: 16,
            baseDmg: 0,
            cost: 0,
            null
        );
    }
    public static StudMove Skip
    {
        get => new StudMove(
            name: "Skipped!",
            id: 17,
            baseDmg: 0,
            cost: 0,
            null
        );
    }
}

[Flags]
public enum MoveEffect
{
    [Description("None")]
    None = 0,
    [Description("Atk UP")]
    AtkUp = 1,
    [Description("2x Cost")]
    DoubleCost = 2,
    [Description("Low HP Dmg")]
    ExtraDmgHealth = 4,
    [Description("Shield")]
    Shield = 8,
    [Description("Stun")]
    Stun = 16
}

public enum StudType
{
    [Description("PIMPY Son OPP")]
    PimpySonOpp = 0,
    [Description("Chum Lee")]
    ChumLee     = 1,
    [Description("Redrum")]
    Redrum      = 2,
    [Description("Mr. Miagi")]
    MrMiagi     = 3,
    [Description("Bape")]
    Bape        = 4,
    [Description("Doughboy")]
    Doughboy    = 5,
    [Description("Tole Tole")]
    ToleTole    = 6
}

public static class StringExtensions
{
    // Shoutouts to https://stackoverflow.com/questions/1415140/can-my-enums-have-friendly-names
    public static string GetDescription(this Enum value)
    {
        Type type = value.GetType();
        string name = Enum.GetName(type, value);
        if (name != null)
        {
            FieldInfo field = type.GetField(name);
            if (field != null)
            {
                DescriptionAttribute attr =
                       Attribute.GetCustomAttribute(field,
                         typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attr != null)
                {
                    return attr.Description;
                }
            }
        }
        return null;
    }
}