using UnityEngine;

public interface ISpellEffect
{
    void Execute(Vector3 position, int value, float range);
}