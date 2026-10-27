using System;
using System.Collections.Generic;
using UnityEngine;

// P2 - Inventaire du joueur.
public class Inventory : MonoBehaviour
{
    readonly Dictionary<ItemType, int> items = new Dictionary<ItemType, int>();

    public ItemType Equipped { get; private set; } = ItemType.None;
    public bool CompassEquipped { get; private set; }
    public event Action Changed;

    public int Count(ItemType t) => items.TryGetValue(t, out int c) ? c : 0;

    public void Add(ItemType t, int n = 1)
    {
        if (t == ItemType.None || n <= 0) return;
        items[t] = Count(t) + n;
        Changed?.Invoke();
    }

    public bool Has(ItemStack[] cost)
    {
        foreach (var s in cost)
            if (Count(s.type) < s.count) return false;
        return true;
    }

    public bool Remove(ItemType t, int n = 1)
    {
        if (Count(t) < n) return false;
        items[t] = Count(t) - n;
        if (items[t] == 0) items.Remove(t);
        if (Equipped == t && Count(t) == 0) Equipped = ItemType.None;
        Changed?.Invoke();
        return true;
    }

    public bool Spend(ItemStack[] cost)
    {
        if (!Has(cost)) return false;
        foreach (var s in cost)
        {
            items[s.type] -= s.count;
            if (items[s.type] == 0) items.Remove(s.type);
        }
        Changed?.Invoke();
        return true;
    }

    public bool Craft(Recipe r)
    {
        if (!Spend(r.cost)) return false;
        Add(r.result);
        if (r.result == ItemType.Compass) SetCompass(true);
        else Equip(r.result);
        return true;
    }

    public void Equip(ItemType t)
    {
        if (t != ItemType.None && (!ItemDB.IsTool(t) || Count(t) == 0)) return;
        Equipped = t;
        Changed?.Invoke();
    }

    public void SetCompass(bool on)
    {
        CompassEquipped = on && Count(ItemType.Compass) > 0;
        Changed?.Invoke();
    }

    public void Clear()
    {
        items.Clear();
        Equipped = ItemType.None;
        CompassEquipped = false;
        Changed?.Invoke();
    }

    public List<ItemStack> ToList()
    {
        var list = new List<ItemStack>();
        foreach (var kv in items) list.Add(new ItemStack(kv.Key, kv.Value));
        return list;
    }

    public void Load(List<ItemStack> list, ItemType equipped, bool compass)
    {
        items.Clear();
        foreach (var s in list)
            if (s.type != ItemType.None && s.count > 0) items[s.type] = s.count;
        Equipped = ItemDB.IsTool(equipped) && Count(equipped) > 0 ? equipped : ItemType.None;
        CompassEquipped = compass && Count(ItemType.Compass) > 0;
        Changed?.Invoke();
    }
}
