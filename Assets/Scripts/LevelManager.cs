using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [field: SerializeField] public List<Sprite> NumberSprites { get; private set; }
    public SpriteRenderer spriteRenderer;
    public int number;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        number = Random.Range(0, 10);
        spriteRenderer.sprite = NumberSprites[number];
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
