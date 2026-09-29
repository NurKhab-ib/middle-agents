using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MiddleAgents
{
    public sealed class TerrainWorld : MonoBehaviour
    {
        [SerializeField, Range(96, 512)] private int mapWidth = 384;
        [SerializeField, Range(64, 384)] private int mapHeight = 256;
        [SerializeField] private int startingSeed = 0;

        private SpriteRenderer mapRenderer;
        private Sprite currentSprite;
        private Texture2D currentTexture;
        private int generation;

        private void Start()
        {
            GameObject mapObject = new GameObject("Generated Terrain");
            mapObject.transform.SetParent(transform, false);
            mapRenderer = mapObject.AddComponent<SpriteRenderer>();
            mapRenderer.sortingOrder = -10;

            Camera view = Camera.main;
            if (view != null && view.GetComponent<SpectatorCamera>() == null)
                view.gameObject.AddComponent<SpectatorCamera>();

            Generate(startingSeed == 0 ? Environment.TickCount : startingSeed);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                generation++;
                Generate(unchecked(Environment.TickCount ^ generation * 7919));
            }
        }

        private void Generate(int seed)
        {
            TerrainMap map = TerrainAgents.Generate(mapWidth, mapHeight, seed);
            Sprite nextSprite = TerrainPainter.Paint(map, seed, out Texture2D nextTexture);
            mapRenderer.sprite = nextSprite;

            if (currentSprite != null) Destroy(currentSprite);
            if (currentTexture != null) Destroy(currentTexture);
            currentSprite = nextSprite;
            currentTexture = nextTexture;
        }

        private void OnDestroy()
        {
            if (currentSprite != null) Destroy(currentSprite);
            if (currentTexture != null) Destroy(currentTexture);
        }
    }
}
