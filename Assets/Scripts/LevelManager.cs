using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    [field: SerializeField] public List<Sprite> NumberSprites { get; private set; }
    public Image spriteRenderer;
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
