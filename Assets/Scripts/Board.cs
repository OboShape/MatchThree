using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        FillBoard();

        //HighLightMatches();
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

    GamePiece FillRandomAt(int x, int y)
    {
        GameObject randomPiece = Instantiate(GetRandomGamePiece(), Vector3.zero, Quaternion.identity, m_gamePieceContainer.transform) as GameObject;
        if (randomPiece != null)
        {
            PlaceGamePiece(randomPiece.GetComponent<GamePiece>(), x, y);
            randomPiece.GetComponent<GamePiece>().Init(this, moveDuration);
            return randomPiece.GetComponent<GamePiece>();
        }
        return null;
    }

    void FillBoard()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                GamePiece piece =  FillRandomAt(i, j);

                int maxIterations = 100;
                int iteration = 0;
                while (HasMatchOnFill(i, j))
                {
                    ClearPieceAt(i, j);
                    piece = FillRandomAt(i, j);
                    iteration++;

                    if (iteration >= maxIterations)
                    {
                        Debug.Log("Match Found : iteration " + iteration);
                        iteration = 0;
                        break;
                    }
                }
            }
        }
    }

   bool HasMatchOnFill(int x, int y, int minLength = 3)
    {
        List<GamePiece> leftMatches = FindMatches(x, y, new Vector2(-1, 0), minLength);
        List<GamePiece> downMatches = FindMatches(x, y, new Vector2(0, -1), minLength);

        if (leftMatches == null) leftMatches = new List<GamePiece>();
        if (downMatches == null) downMatches = new List<GamePiece>();

        return (leftMatches.Count > 0 || downMatches.Count > 0);
    }

    public void CLickTile(Tile tile)
    {
        if (m_clickedTile == null)
        {
            m_clickedTile = tile;
        }
    }

    public void DragToTile(Tile tile)
    {
        if (m_clickedTile != null && IsNextTo(m_clickedTile, tile))
            m_targetTile = tile;
        else
            m_targetTile = null;  // added this so the target is cleared if dragged outwith the adjacent tiles
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
        StartCoroutine(SwitchTilesRoutine(clickedTile, targetTile));
    }

    IEnumerator SwitchTilesRoutine(Tile clickedTile, Tile targetTile)
    {
        GamePiece clickedPiece = m_allGamePieces[clickedTile.xIndex, clickedTile.yIndex];
        GamePiece targetPiece = m_allGamePieces[targetTile.xIndex, targetTile.yIndex];

        if (targetPiece != null && clickedPiece != null)
        {
            clickedPiece.Move(targetTile.xIndex, targetTile.yIndex);
            targetPiece.Move(clickedTile.xIndex, clickedTile.yIndex);

            yield return new WaitForSeconds(moveDuration);

            List<GamePiece> clickedPieceMatches = FindMatchesAt(clickedTile.xIndex, clickedTile.yIndex);
            List<GamePiece> targetPieceMatches = FindMatchesAt(targetTile.xIndex, targetTile.yIndex);

            if (targetPieceMatches.Count == 0 && clickedPieceMatches.Count == 0)
            {
                clickedPiece.Move(clickedTile.xIndex, clickedTile.yIndex);
                targetPiece.Move(targetTile.xIndex, targetTile.yIndex);
            }
            else
            {
                yield return new WaitForSeconds(moveDuration);
                ClearPieceAt(clickedPieceMatches);
                ClearPieceAt(targetPieceMatches);
                //HighlightMatchesAt(clickedTile.xIndex, clickedTile.yIndex);
                //HighlightMatchesAt(targetTile.xIndex, targetTile.yIndex);
            }

            
        }
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

    List <GamePiece> FindMatches (int startX, int startY, Vector2 searchDirection, int minLength = 3)
    {
        List<GamePiece> matches = new List<GamePiece>();
        GamePiece startPiece = null;

        if (IsWithinBounds(startX, startY))
        {
            startPiece = m_allGamePieces[startX, startY];
        }

        if (startPiece != null)
        {
            matches.Add(startPiece);
        }
        else
        {
            return null;
        }

        int nextX, nextY;

        int maxValue = (width > height) ? width: height;

        for (int i = 1; i < maxValue; i++)
        {
            nextX = startX + (int)Mathf.Clamp(searchDirection.x, -1, 1) * i;
            nextY = startY + (int)Mathf.Clamp(searchDirection.y, -1, 1) * i;

            if (!IsWithinBounds(nextX, nextY))
            {
                break;
            }

            GamePiece nextPiece = m_allGamePieces[nextX, nextY];

            if (nextPiece == null)
            {
                break;
            }
            else
            {
                if (nextPiece.matchValue == startPiece.matchValue && !matches.Contains(nextPiece))
                {
                    matches.Add(nextPiece);
                }
                else
                {
                    break;
                }
            }
            
        }

        if (matches.Count >= minLength)
        {
            return matches;
        }

        return null;
    }

    List <GamePiece> FindVerticalMatches (int startX, int startY, int minLength = 3)
    {
        List<GamePiece> upwardMatches = FindMatches(startX, startY, new Vector2(0, 1), 2);
        List<GamePiece> downwardMatches = FindMatches(startX, startY, new Vector2(0, -1), 2);

        if (upwardMatches == null) upwardMatches = new List<GamePiece>();
        if (downwardMatches == null) downwardMatches = new List<GamePiece>();

        List<GamePiece> combinedMatches = upwardMatches.Union(downwardMatches).ToList();
        return (combinedMatches.Count >= minLength) ? combinedMatches : null;
    }

    List<GamePiece> FindHorizontalMatches(int startX, int startY, int minLength = 3)
    {
        List<GamePiece> leftMatches = FindMatches(startX, startY, new Vector2(-1, 0), 2);
        List<GamePiece> rightMatches = FindMatches(startX, startY, new Vector2(1, 0), 2);

        if (leftMatches == null) leftMatches = new List<GamePiece>();
        if (rightMatches == null) rightMatches = new List<GamePiece>();

        List<GamePiece> combinedMatches = leftMatches.Union(rightMatches).ToList();
        return (combinedMatches.Count >= minLength) ? combinedMatches : null;
    }

    void HighLightTileOff(int x, int y)
    {
        SpriteRenderer spriteRenderer = m_allTiles[x, y].GetComponent<SpriteRenderer>();
        spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, 0f);
    }

    void HighLightTileOn(int x, int y, Color color)
    {
        SpriteRenderer spriteRenderer = m_allTiles[x, y].GetComponent<SpriteRenderer>();
        spriteRenderer.color = color;
    }

    private List<GamePiece> FindMatchesAt(int x, int y, int minLength = 3)
    {
        List<GamePiece> horizMatches = FindHorizontalMatches(x, y, minLength);
        List<GamePiece> vertMatches = FindVerticalMatches(x, y, minLength);

        if (horizMatches == null) horizMatches = new List<GamePiece>();
        if (vertMatches == null) vertMatches = new List<GamePiece>();

        List<GamePiece> combinedMatches = horizMatches.Union(vertMatches).ToList();
        return combinedMatches;
    }

    private void HighlightMatchesAt(int x, int y)
    {
        HighLightTileOff(x, y);

        List<GamePiece> combinedMatches = FindMatchesAt(x, y);

        if (combinedMatches.Count > 0)
        {
            foreach (GamePiece piece in combinedMatches)
            {
                HighLightTileOn(piece.xIndex, piece.yIndex, piece.GetComponent<SpriteRenderer>().color);
            }
        }
    }

    void HighLightMatches()
    {
        for (int i=0; i <width; i++)
        {
            for(int j = 0; j < height; j++)
            {
                HighlightMatchesAt(i, j);
            }
        }
    }

    void ClearPieceAt(int x, int y)
    {
        GamePiece pieceToClear = m_allGamePieces[x, y];

        if (pieceToClear != null)
        {
            m_allGamePieces[x, y] = null;
            Destroy(pieceToClear.gameObject);
        }

        HighLightTileOff(x, y);
    }

    void ClearPieceAt(List<GamePiece> gamePieces)
    {
        foreach (GamePiece piece in gamePieces)
        {
            ClearPieceAt(piece.xIndex, piece.yIndex);
        }
    }

    void ClearBoard()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                ClearPieceAt(i, j);
            }
        }
    }
   
}
