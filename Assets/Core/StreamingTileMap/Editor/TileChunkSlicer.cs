using System;
using System.Diagnostics.Contracts;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Core
{
    public class Map2D<T>
    {
        private T[,] mMapData;
        private int mWidth;
        private int mHeight;

        public Map2D(int width, int height)
        {
            mWidth = width;
            mHeight = height;
            mMapData = new T[width, height];
        }

        public Map2D(uint width, uint height)
        {
            mWidth = (int)width;
            mHeight = (int)height;
            mMapData = new T[width, height];
        }

        public int Width => mWidth;
        public int Height => mHeight;

        /// <summary>
        ///
        /// </summary>
        /// <param name="MapX">X pos in map coord</param>
        /// <param name="MapY">Y pos in map coord</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public T Get(int MapX, int MapY)
        {
            if (!IsValidCoordinate(MapX, MapY))
                throw new ArgumentOutOfRangeException("[Map_2D.cs] Invalid coordinates.");
            return mMapData[MapX, MapY];
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="MapX">X pos in map coord</param>
        /// <param name="MapY">Y pos in map coord</param>
        /// <param name="value"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void Set(int MapX, int MapY, T value)
        {
            if (!IsValidCoordinate(MapX, MapY))
            {
                Debug.Log("cuo:" + MapX + ", " + MapY);
                throw new ArgumentOutOfRangeException("[Map_2D.cs] Invalid coordinates.");
            }

            mMapData[MapX, MapY] = value;
        }

        public void Set(Vector2Int pos, T value)
        {
            Set(pos.x, pos.y, value);
        }

        public bool IsValidCoordinate(int x, int y)
        {
            return x >= 0 && x < mWidth && y >= 0 && y < mHeight;
        }

        public void Clear()
        {
            Array.Clear(mMapData, 0, mMapData.Length);
        }

        public void ForEachRun(Action<int, int, T> action)
        {
            for (int x = 0; x < mWidth; x++)
            {
                for (int y = 0; y < mHeight; y++)
                {
                    action(x, y, mMapData[x, y]);
                }
            }
        }

        public void FillAll(T value)
        {
            for (int x = 0; x < mWidth; x++)
            {
                for (int y = 0; y < mHeight; y++)
                {
                    mMapData[x, y] = value;
                }
            }
        }

        public T[,] GetRawMapData()
        {
            return (T[,])mMapData.Clone();
        }

        public void Resize(int newWidth, int newHeight)
        {
            T[,] newMapData = new T[newWidth, newHeight];
            int minWidth = Math.Min(mWidth, newWidth);
            int minHeight = Math.Min(mHeight, newHeight);

            for (int x = 0; x < minWidth; x++)
            {
                for (int y = 0; y < minHeight; y++)
                {
                    newMapData[x, y] = mMapData[x, y];
                }
            }

            mMapData = newMapData;
            mWidth = newWidth;
            mHeight = newHeight;
        }
    }

    public class SerializedTileData
    {
        public TileBase mTileBase;
    }


    public class VgTile : TileBase
    {
        
    }

    public class TileChunkSlicer : MonoBehaviour
    {
        private void Start()
        {
            var tileMap = FindAnyObjectByType<Tilemap>();
            Debug.Log(tileMap.cellBounds);
            Debug.Log(tileMap.localBounds);
            SliceTileMapToChunk(tileMap);

            Debug.Log(mChunkMap);
        }

        [Pure]
        public void SliceTileMapToChunk(Tilemap tilemapToSlice)
        {
            var tilemapBounds = tilemapToSlice.cellBounds;
            var boundSize = tilemapBounds.size;

            int chunkCountX = boundSize.x / CHUNK_SIZE + 1;
            int chunkCountY = boundSize.y / CHUNK_SIZE + 1;

            mChunkMap = new Map2D<Map2D<Tile>>(chunkCountX, chunkCountY);
            for (int y = 0; y < chunkCountY; y++)
            {
                for (int x = 0; x < chunkCountX; x++)
                {
                    var chunk = new Map2D<Tile>(CHUNK_SIZE, CHUNK_SIZE);
                    var chunkBounds = new BoundsInt();
                    chunkBounds.size = new Vector3Int(CHUNK_SIZE, CHUNK_SIZE, 0);
                    chunkBounds.position +=
                        new Vector3Int(x, y, 0) * new Vector3Int(CHUNK_SIZE, CHUNK_SIZE, 0);
                    SaveTileChunk(chunkBounds, tilemapToSlice, ref chunk);
                    mChunkMap.Set(new Vector2Int(x, y), chunk);
                }
            }
        }

        [Pure]
        public void SaveTileChunk(
            BoundsInt chunkBounds,
            Tilemap tilemapToSlice,
            ref Map2D<Tile> container
        )
        {
            var tilemapBounds = tilemapToSlice.cellBounds;
            var minY = Mathf.Max(chunkBounds.min.y, tilemapBounds.min.y);
            var minX = Mathf.Max(chunkBounds.min.x, tilemapBounds.min.x);
            var maxY = Mathf.Min(chunkBounds.max.y, tilemapBounds.max.y);
            var maxX = Mathf.Min(chunkBounds.max.x, tilemapBounds.max.x);

            for (int y = minY; y < maxY; y++)
            {
                for (int x = minX; x < maxX; x++)
                {
                    var tile = tilemapToSlice.GetTile<Tile>(new Vector3Int(x, y, 0)); // TODO(vanish): this seems to be very slow
                    container.Set(new Vector2Int(x, y), tile);
                }
            }
        }

        public const int CHUNK_SIZE = 64;
        public Map2D<Map2D<Tile>> mChunkMap;
    }
}
