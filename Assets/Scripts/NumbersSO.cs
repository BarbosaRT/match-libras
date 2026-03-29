using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NumbersSO", menuName = "Scriptable Objects/NumbersSO")]
public class NumbersSO : ScriptableObject
{
    [field: SerializeField] public List<Sprite> NumberSprites { get; private set; }
}
