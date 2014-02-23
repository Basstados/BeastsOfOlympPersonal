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
		if (attack.range == 0) {
			target = source.mapTile;
		}
		// quite early if source unit is not allowed to attack or target is out of range
		if(!source.CanAttack || AttackDistance(source.mapTile, target) > attack.range)
			return;
		
		// lower attack resource on source unit
		source.AttackPoints = 0;

		float hit = (float) new Random().NextDouble();
		int damage = 0;
		byte efficency = 1;
		float typeModifier = 1;

		// check hit chance
		if(attack.hitChance >= hit) {
			// attack is succesfull
			// apply damage to all unit in attack area
			int x = 0;
			int y = 0;
			List<Unit> victims = new List<Unit>();
			Vector rotDir = new Vector(target.x - source.mapTile.x, target.y - source.mapTile.y);
			rotDir.NormalizeTo4Direction();
			int[,] rot = Vector.RotateToMatrix(rotDir);
			foreach(Vector pt in attack.area) {
				Vector rotPt = pt.Clone();
				if(rotDir != Vector.zero)
					rotPt.ApplyMatrix(rot);
				x = target.x + rotPt.x;
				y = target.y + rotPt.y;
				if(model.IsPointOnGrid(new Vector(x,y)))
					if(model.mapTiles[x][y].unit != null) {
						Unit unit = model.mapTiles[x][y].unit;

						typeModifier = CalcTypeModifier(unit);
						efficency = (byte) ((typeModifier > 1f) ? 2 : (typeModifier == 1f) ? 1 : 0);
						damage = CalcDamage(source.Attack, attack.damage, typeModifier);


						unit.LoseHealth(damage);
						victims.Add(unit);
					}
			}
			EventProxyManager.FireEvent(this, new UnitAttackedEvent(attack,source,target, victims,efficency, damage));

			// when target died fire event AFTER attack was performed
			foreach(Unit u in victims) {
				if(u.HealthPoints <= 0) {
					// remove target from map
					u.mapTile.unit = null;
					// fire event
					EventProxyManager.FireEvent(this, new UnitDiedEvent(u));
				}
			}
		}
	}

	private int AttackDistance(MapTile from, MapTile to)
	{
		return Math.Abs (from.x - to.x) + Math.Abs (from.y - to.y);
	}

	private float CalcTypeModifier(Unit unit)
	{
		// calculate type modifier
		float typeModifier = 1f;
		if(attack.type.strengths.Length > 0)
		if(Array.Exists(attack.type.strengths, delegate(string t) { return t == unit.data.type.name; })) {
			// modifier when type is very effectiv
			typeModifier *= 2f;
		}
		if(attack.type.weaknesses.Length > 0)
		if(Array.Exists(attack.type.weaknesses, delegate(string t) { return t == unit.data.type.name; })) {
			// modifier when type is not effectiv
			typeModifier *= 0.5f;
		}
		return typeModifier;
	}

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
	public int damage;

	public UnitAttackedEvent (Attack attack, Unit source, MapTile target, List<Unit> victims, byte efficieny, int damage)
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
