using System;

public class BurningOilField : Topping
{
    int damagePerTurn = 1;

    /// <summary>
    /// Determines whether this topping in blocking the field or not.
    /// Oil field are passable
    /// </summary>
    /// <returns><c>true</c> if this topping is blocking; otherwise, <c>false</c>.</returns>
    public override bool IsBlocking()
    {
        return false;
    }

    /// <summary>
    /// The effect that will happen, if a unit stands on the field with this topping.
    /// Deal damage each turn to the unit on this burning oil.
    /// </summary>
    /// <param name="mapTile">The mapTile the obstacle and the unit stand on.</param>
    /// <param name="unit">The unit wich will effected by what ever will happen.</param>
    public override void OnStayEffect(MapTile mapTile, Unit unit)
    {
        unit.LoseHealth(damagePerTurn);
    }

    /// <summary>
    /// The effect hat will happen, if this field will be hit by an attack.
    /// </summary>
    /// <param name="mapTile">The mapTile the obstacle and the unit stand on.</param>
    /// <param name="attack">The attack wich triggerd this effect.</param>
    public override void OnAttackEffect(MapTile mapTile, Attack attack)
    {

    }
}
