using System;
using System.Collections.Generic;

public class CAttackUnit : ICommand
{
	Attack attack;
	Unit source;
	MapTile target;
	Model model;
	Controller controller;

	public CAttackUnit(Unit source, MapTile target, Attack attack, Model model, Controller controller)
	{
		this.source = source;
		this.attack = attack;
		this.target = target;
		this.model = model;
		this.controller = controller;
	}

	public void Execute ()
	{
		// auto set attack target for range 0 attack
		// helps AI to perfome rang 0 attacks
		if (attack.range == 0) {
			target = source.mapTile;
		}
		// quite early if source unit is not allowed to attack or target is out of range
		if(!source.CanAttack || AttackDistance(source.mapTile, target) > attack.range)
			return;
		
		// lower attack resource on source unit
		// so it can't attack again this turn
		source.AttackPoints = 0;

		float hit = (float) new Random().NextDouble();
		int damageUnit = 0;
		byte efficency = 1;
		float typeModifier = 1;

		// check hit chance
		if(attack.hitChance < hit)  return;

		// attack is succesfull
		// apply damage to all unit in attack area
		// don't trigger the toppings right away, otherwise breaking oil vases would ignite right away in most cases
		List<KeyValuePair<MapTile, Topping> > affectedToppings = new List<KeyValuePair<MapTile, Topping> >();
		// to be secure we make a list of already checked mapTiles, so the recusion will not be endless
		List<Vector> checkedMapTiles = new List<Vector>();
		int x = 0;
		int y = 0;
		List<Unit> victims = new List<Unit>();
		List<int> damage = new List<int>();
		Vector rotDir = new Vector(target.x - source.mapTile.x, target.y - source.mapTile.y);
		// make sure the direction for the attack is one of the four standart direcitons
		rotDir.NormalizeTo4Direction();
		int[,] rot = Vector.RotateToMatrix(rotDir);
		// perform attack for eacht field in the attack area
		foreach(Vector pt in attack.area) {
			// apply rotation to field
			Vector rotPt = pt.Clone();
			if(rotDir != Vector.zero)
				rotPt.ApplyMatrix(rot);
			x = target.x + rotPt.x;
			y = target.y + rotPt.y;
			// check if field is inside grid
			if(model.IsPointOnGrid(new Vector(x,y))) {
				// do attack effect on mapTile
            if (model.mapTiles[x][y].topping != null)
            {
                //recursivly check all neighbour fields if they are of the same topping type
                RecursivlyEffectToppings(model.mapTiles[x][y], attack, ref checkedMapTiles, ref affectedToppings);
            }
					

				// check field for units
				if(model.mapTiles[x][y].unit != null) {
					Unit unit = model.mapTiles[x][y].unit;

					// calculate and apply damage to unit
					typeModifier = CalcTypeModifier(attack, unit);
					efficency = (byte) ((typeModifier > 1f) ? 2 : (typeModifier == 1f) ? 1 : 0);
					damageUnit = CalcDamage(source.Attack, attack.damage, typeModifier);

					
					// add to list of hit units
					victims.Add(unit);
					damage.Add(damageUnit);
				}
			}

		}

		// apply the attack effect to all hit toppings
		foreach (KeyValuePair<MapTile, Topping> pair in affectedToppings)
		{
			// check that the topping still exists and wasn't destroyed by a neighbouring topping
			if (pair.Key.topping == pair.Value)
			{
				pair.Value.OnAttackEffect(attack, model);
			}
		}

		EventProxyManager.FireEvent(this, new UnitAttackedEvent(attack,source,target, victims,efficency, damage));
		
		for(int i=0; i<victims.Count; i++) {
			victims[i].LoseHealth(damage[i]);
		}

		// when target died fire event AFTER attack was performed
		this.model.combat.CheckForDeadUnits();
	}
	
	private void RecursivlyEffectToppings(MapTile mapTile, Attack attack, ref List<Vector> checkedMapTiles, ref List<KeyValuePair<MapTile, Topping> > affectedToppings)
    {
		// When the topping is not linked, the effect will not spread and we return here
		bool linked = (mapTile.topping.IsLinked);

		// fire effects spread differently
		if (attack.element.elementName == "Feuer")
		{
			linked = (mapTile.topping.IsLinkedForFire);
		}

        // stopping condition
        if (checkedMapTiles.Contains(new Vector(mapTile.x, mapTile.y)))
            return;

        // save which type we are looking for
        Type toppingType = mapTile.topping.GetType();

		// remember the map tile as triggered
		affectedToppings.Add(new KeyValuePair<MapTile, Topping>(mapTile, mapTile.topping));
        checkedMapTiles.Add(new Vector(mapTile.x, mapTile.y));

		if(!linked) return;

        // list of all relative neighbours
        Vector[] nghb = new Vector[] {new Vector(1,0), new Vector(0,-1), new Vector(-1,0), new Vector(0,1)};

        // loop over all neighbours
        foreach (Vector vec in nghb)
        {
            Vector pos = new Vector(mapTile.x + vec.x, mapTile.y + vec.y);
            if (model.IsPointOnGrid(pos))
            {
                if (model.mapTiles[pos.x][pos.y].topping != null)
                {
                    if (model.mapTiles[pos.x][pos.y].topping.GetType() == toppingType)
                    {
                        // this neighbour mapTile has the same topping, so spread the effect
						RecursivlyEffectToppings(model.mapTiles[pos.x][pos.y], attack, ref checkedMapTiles, ref affectedToppings);
                    }
                }
            }
        }
    }

	/// <summary>
	/// Calculate the distance between to maptile with manhatten norm, ignoring its weights
	/// </summary>
	/// <returns>The distance.</returns>
	/// <param name="from">From.</param>
	/// <param name="to">To.</param>
	private int AttackDistance(MapTile from, MapTile to)
	{
		return Math.Abs (from.x - to.x) + Math.Abs (from.y - to.y);
	}

	/// <summary>
	/// Calculates the type modifier.
	/// </summary>
	/// <returns>The type modifier.</returns>
	/// <param name="attack">Perfomed attack</param>
	/// <param name="unit">Target unit</param>
	private float CalcTypeModifier(Attack attack, Unit unit)
	{
		// calculate type modifier
		float typeModifier = 1f;
		if(attack.element.strengths.Length > 0)
			if(Array.Exists(attack.element.strengths, delegate(Element t) { return t.elementName == unit.Element.elementName; })) {
				// modifier when type is very effectiv
				typeModifier *= 1.5f;
			}
		if(attack.element.weaknesses.Length > 0)
			if(Array.Exists(attack.element.weaknesses, delegate(Element t) { return t.elementName == unit.Element.elementName; })) {
				// modifier when type is not effectiv
				typeModifier *= 0.5f;
			}
		return typeModifier;
	}

	/// <summary>
	/// The damage formular
	/// </summary>
	/// <returns>The damage.</returns>
	/// <param name="sourceAtkValue">Attack value of source unit</param>
	/// <param name="atkDmg">Damage of the performed attack</param>
	/// <param name="typeModifier">The type modifier.</param>
	private int CalcDamage(int sourceAtkValue, int atkDmg, float typeModifier) {
		// the actual damage formular
		return (int) Math.Round((sourceAtkValue + atkDmg) * typeModifier);
	}
}

public class UnitAttackedEvent : EventProxyArgs 
{
	public Attack attack;
	public Unit source;
	public MapTile target;
	public List<Unit> victims;
	public byte efficiency;
	public List<int> damage;

	public UnitAttackedEvent (Attack attack, Unit source, MapTile target, List<Unit> victims, byte efficieny, List<int> damage)
	{
		this.name = EventName.UnitAttacked;
		this.attack = attack;
		this.source = source;
		this.target = target;
		this.victims = victims;
		this.efficiency = efficieny;
		this.damage = damage;
	}
	
}

public class UnitDiedEvent : EventProxyArgs
{
	public Unit unit;

	public UnitDiedEvent (Unit unit)
	{
		this.name = EventName.UnitDied;
		this.unit = unit;
	}
}
