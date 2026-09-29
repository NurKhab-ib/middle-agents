using System;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class TerrainWorld : MonoBehaviour {
    // the same size is used for map width and height
    [SerializeField] private int mapSize = 400;
    
    [SerializeField] private int startingSeed = 0;

    private SpriteRenderer mapRenderer;
    private Sprite currentSprite;
    private Texture2D currentTexture;
    private int generation;

    private void Start() {
        // keep the generated sprite under one scene object
        GameObject mapObject = new GameObject("Generated Terrain");
        mapObject.transform.SetParent(transform, false);
        mapRenderer = mapObject.AddComponent<SpriteRenderer>();
        mapRenderer.sortingOrder = -10;

        // the scene camera becomes a free camera when play starts
        Camera view = Camera.main;
        if (view != null && view.GetComponent<SpectatorCamera>() == null) {
            view.gameObject.AddComponent<SpectatorCamera>();
        }

        Generate(startingSeed == 0 ? Environment.TickCount : startingSeed);
    }

    private void Update() {
        // r is the only action that changes the map during play
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) {
            generation++;
            Generate(unchecked(Environment.TickCount ^ generation * 7919));
        }
    }

    private void Generate(int seed) {
        // agents make the data first; the painter turns it into one sprite
        var map = TerrainAgents.Generate(mapSize, mapSize, seed);
        var nextSprite = TerrainPainter.Paint(map, seed, out Texture2D nextTexture);
        
        mapRenderer.sprite = nextSprite;

        // drop the old runtime assets after the new sprite is ready
        if (currentSprite) {
            Destroy(currentSprite);
        }

        if (currentTexture) {
            Destroy(currentTexture);
        }

        currentSprite = nextSprite;
        currentTexture = nextTexture;
    }

    private void OnDestroy() {
        // these textures live only while the scene object lives
        if (currentSprite != null) {
            Destroy(currentSprite);
        }

        if (currentTexture != null) {
            Destroy(currentTexture);
        }
    }
}
