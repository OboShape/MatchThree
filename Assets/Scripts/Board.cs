using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    public int width;
    public int height;
    public int borderPadding = 0;

    public GameObject tilePrefab;
    public GameObject[] gamePiecePrefabs;

    // empty container objects
    GameObject m_tileContainer;
    GameObject m_gamePieceContainer;

    Tile [,] m_allTiles;
    GamePiece[,] m_allGamePieces;

    Tile m_clickedTile;
    Tile m_targetTile;

    public float moveDuration = 0.2f;
   
 
    void Start()
    {
        // create a couple of empties to be parent holders for Tiles and GamePiesces
        m_tileContainer = new GameObject("Tile Container");
        m_gamePieceContainer = new GameObject("GamPiece Container");

        m_tileContainer.transform.SetParent(this.transform);
        m_gamePieceContainer.transform.SetParent(this.transform);


        m_allTiles = new Tile[width, height];
        SetupTiles();

        m_allGamePieces = new GamePiece[width, height];
        SetupCamera();

        FillRandom();
    }

    void SetupTiles()
    {
        for (int i = 0; i < width; i++ )
        {
            for ( int j = 0; j < height; j++)
            {
                GameObject tile = Instantiate(tilePrefab, new Vector3(i, j, 0.0f), Quaternion.identity, m_tileContainer.transform) as GameObject;
                tile.name = "Tile ( " + i + "," + j + " )";
                m_allTiles[i, j] = tile.GetComponent<Tile>();
                m_allTiles[i, j].Init(i, j, this);
            }
        }
    }

    void SetupCamera()
    {
        Camera.main.transform.position = new Vector3((float)(width-1) / 2f, (float)(height-1) / 2f, -10f);

        float aspectRatio = (float)Screen.width / (float)Screen.height;

        float cameraSizeVertical = height / 2.0f + borderPadding;
        float cameraSizeHorizontal = (((float)width / 2f + (float)borderPadding) / aspectRatio);

        Camera.main.orthographicSize = (cameraSizeHorizontal > cameraSizeVertical) ? cameraSizeHorizontal : cameraSizeVertical;
    }

    public GameObject GetRandomGamePiece()
    {
        int randomIndex = Random.Range(0, gamePiecePrefabs.Length);
        if (gamePiecePrefabs[randomIndex] == null)
        {
            Debug.LogWarning("BOARD : " + randomIndex + " does not contain a valid gamepiece prefab!");
        }
        return gamePiecePrefabs[randomIndex];
    }

    public void PlaceGamePiece(GamePiece gamePiece, int x, int y)
    {
        if (gamePiece == null)
        {
            Debug.LogWarning("BOARD : Invalid GamePiece!");
        }

        gamePiece.transform.position = new Vector3(x, y, 0f);
        gamePiece.transform.rotation = Quaternion.identity;
        if (IsWithinBounds(x, y))
        {
            m_allGamePieces[x, y] = gamePiece;
        }
        gamePiece.SetCoord(x, y);
    }

    bool IsWithinBounds (int x, int y)
    {
        return (x >= 0 && x < width && y >= 0 && y < height);
    }

    void FillRandom()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                GameObject randomPiece = Instantiate(GetRandomGamePiece(), Vector3.zero, Quaternion.identity, m_gamePieceContainer.transform) as GameObject;
                if (randomPiece != null)
                {
                    PlaceGamePiece(randomPiece.GetComponent<GamePiece>(), i, j);
                    randomPiece.GetComponent<GamePiece>().Init(this, moveDuration);
                }
            }
        }
    }

    public void CLickTile(Tile tile)
    {
        if (m_clickedTile == null)
        {
            m_clickedTile = tile;
            //Debug.Log("Clicked Tile : " + tile.name);
        }
    }

    public void DragToTile(Tile tile)
    {
        if (m_clickedTile != null && IsNextTo(m_clickedTile, tile))
            m_targetTile = tile;
    }

    public void ReleaseTile()
    {
        if (m_clickedTile != null && m_targetTile != null)
        {
            SwitchTiles(m_clickedTile, m_targetTile);
        }
        m_clickedTile = null;
        m_targetTile = null;
    }

    void SwitchTiles(Tile clickedTile, Tile targetTile)
    {
        GamePiece clickedPiece = m_allGamePieces[clickedTile.xIndex, clickedTile.yIndex];
        GamePiece targetPiece = m_allGamePieces[targetTile.xIndex, targetTile.yIndex];

        clickedPiece.Move(targetTile.xIndex, targetTile.yIndex);
        targetPiece.Move(clickedTile.xIndex, clickedTile.yIndex);
    }

    bool IsNextTo(Tile start, Tile end)
    {
        if (Mathf.Abs(start.xIndex - end.xIndex) == 1 && start.yIndex == end.yIndex)
        {
            return true;
        }
        if (Mathf.Abs(start.yIndex - end.yIndex) == 1 && start.xIndex == end.xIndex)
        {
            return true;
        }
        return false;
    }
}
