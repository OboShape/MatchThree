using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePiece : MonoBehaviour
{
    public int xIndex;
    public int yIndex;

    Board m_board;

    bool m_isMoving = false;
    float m_moveDuration = .2f;

    public InterpType interpolation = InterpType.SmootherStep;

    public enum InterpType
    {
        Linear,
        EaseOut,
        EaseIn,
        SmoothStep,
        SmootherStep
    };


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.RightArrow))
        //{
        //    Move((int)transform.position.x + 1, (int)transform.position.y, .5f);
        //}
        //if (Input.GetKeyDown(KeyCode.LeftArrow))
        //{
        //    Move((int)transform.position.x - 1, (int)transform.position.y, .5f);
        //}
    }

    public void Init(Board board, float _duration)
    {
        m_board = board;
        m_moveDuration = _duration;
    }

    public void SetCoord(int x, int y)
    {
        xIndex = x;
        yIndex = y;
    }

    public void Move( int destX, int destY)
    {
        if(!m_isMoving)
            StartCoroutine(MoveRoutine(new Vector3(destX, destY, 0), m_moveDuration));
    }

    IEnumerator MoveRoutine(Vector3 destination, float timeToMove)
    {
        Vector3 startPosition = transform.position;

        bool reachedDestination = false;
        float elapsedTime = 0f;

        m_isMoving = true;

        while (!reachedDestination)
        {
            if (Vector3.Distance(transform.position, destination) < 0.01f)
            {
                reachedDestination = true;

                if (m_board != null)
                    m_board.PlaceGamePiece(this, (int)destination.x, (int)destination.y);
                break;
            }

            elapsedTime += Time.deltaTime;
            float t = elapsedTime / timeToMove;

            switch (interpolation)
            {
                case InterpType.Linear:
                    break;
                case InterpType.EaseOut:
                    t = Mathf.Sin(t * Mathf.PI * 0.5f);
                    break;
                case InterpType.EaseIn:
                    t = 1 - Mathf.Cos(t * Mathf.PI + 0.5f);
                    break;
                case InterpType.SmoothStep:
                    t = t * t * (3 - 2 * t);
                    break;
                case InterpType.SmootherStep:
                    t = t * t * t * (t * (t * 6 - 15) + 10);
                    break;
            }
            transform.position = Vector3.Lerp(startPosition, destination, t);

            yield return null;
        }

        m_isMoving = false;
    }
}
