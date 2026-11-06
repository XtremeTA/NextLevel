using System.Collections.Generic;
using UnityEngine;

public sealed class BrickRowManager : MonoBehaviour
{
    [SerializeField] private Brick brickPrefab;
    [SerializeField] private Brick specialBrickPrefab;
    [SerializeField] private Brick bombBrickPrefab;
    [SerializeField] private Brick dragonBrickPrefab;
    [Header("Stage 2")]
    [SerializeField] private Sprite stageTwoBrickIdle;
    [SerializeField] private Sprite[] stageTwoBrickBreakFrames;
    [Header("Stage 3")]
    [SerializeField] private Sprite stageThreeBrickIdle;
    [SerializeField] private Sprite[] stageThreeBrickBreakFrames;
    [Header("Row Spawning")]
    [Tooltip("Chance for an eligible Stage 1 row to contain one ghost brick. Stage 1 never ramps above one.")]
    [SerializeField, Range(0f, 1f)] private float stageOneEnemyRowChance = 0.5f;
    [SerializeField, Min(0)] private int specialBricksPerRow = 1;
    [SerializeField, Min(1f)] private float specialBrickIncreaseInterval = 90f;
    [SerializeField, Min(0)] private int maximumSpecialBricksPerRow = 3;
    [SerializeField] private Brick lifeBrickPrefab;
    [SerializeField, Min(1)] private int rowCount = 4;
    [Tooltip("Starting rows counted from the bottom that contain no enemy bricks.")]
    [SerializeField, Min(0)] private int initialEnemyFreeRows = 2;
    [SerializeField, Min(1)] private int bricksPerRow = 9;
    [SerializeField, Min(0.01f)] private float columnSpacing = 2f;
    [SerializeField, Min(0.01f)] private float rowSpacing = 1f;
    [SerializeField] private Camera playAreaCamera;
    [SerializeField, Min(0.01f)] private float rowShiftDuration = 0.35f;
    [Tooltip("Leftmost brick position. Y is aligned to the camera's top edge when an orthographic camera is available.")]
    [SerializeField] private Vector3 topLeftPosition = new Vector3(-8f, 5.5f, 0f);

    private struct SpawnedBrick
    {
        public Brick brick;
        public Rigidbody2D body;
    }

    // Keep rows in bottom-to-top order, including holes in partially cleared rows.
    private readonly List<SpawnedBrick[]> rows = new List<SpawnedBrick[]>();
    private float brickTopOffset;
    private float shiftElapsed;
    private bool isShifting;
    private float progressionElapsed;
    private int specialSpawnCount;

    public int CurrentSpecialBricksPerRow
    {
        get
        {
            if ((specialBrickPrefab == null && bombBrickPrefab == null && dragonBrickPrefab == null) || specialBricksPerRow <= 0)
                return 0;

            int starting = Mathf.Clamp(specialBricksPerRow, 0, bricksPerRow);
            if (GameManager.Instance == null || GameManager.Instance.CurrentStage < 2)
                return Mathf.Min(1, starting);
            int maximum = Mathf.Clamp(maximumSpecialBricksPerRow, starting, bricksPerRow);
            int increase = Mathf.FloorToInt(progressionElapsed / Mathf.Max(1f, specialBrickIncreaseInterval));
            return Mathf.Min(maximum, starting + increase);
        }
    }

    private void Update()
    {
        if (Time.timeScale > 0f && (GameManager.Instance == null || !GameManager.Instance.IsGameOver))
            progressionElapsed += Time.deltaTime;
    }

    private void Start()
    {
        if (brickPrefab == null || brickPrefab.GetComponent<Rigidbody2D>() == null)
        {
            Debug.LogError("Assign a Brick prefab with a Rigidbody2D to the Brick Row Manager.", this);
            enabled = false;
            return;
        }

        SpriteRenderer spriteRenderer = brickPrefab.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Bounds bounds = spriteRenderer.localBounds;
            brickTopOffset = spriteRenderer.transform.TransformVector(bounds.center + Vector3.up * bounds.extents.y).y;
        }

        Vector3 topLeft = GetTopLeftPosition();
        for (int row = 0; row < rowCount; row++)
        {
            Vector3 leftPosition = topLeft + Vector3.down * rowSpacing * (rowCount - 1 - row);
            rows.Add(SpawnRow(leftPosition, row >= initialEnemyFreeRows));
        }
    }

    private void FixedUpdate()
    {
        if (rows.Count == 0)
            return;

        Vector3 topLeft = GetTopLeftPosition();
        // Wait for actual destruction, including the brick's break animation.
        // Checking only rows[0] prevents a cleared upper row from advancing the board.
        if (!isShifting && IsCleared(rows[0]))
        {
            rows.RemoveAt(0);
            // The new row enters from above the screen as every row slides down.
            rows.Add(SpawnRow(topLeft + Vector3.up * rowSpacing, true));
            shiftElapsed = 0f;
            isShifting = true;
        }

        float duration = Mathf.Max(0.01f, rowShiftDuration);
        if (isShifting)
            shiftElapsed = Mathf.Min(shiftElapsed + Time.fixedDeltaTime, duration);

        float offset = isShifting
            ? rowSpacing * (1f - Mathf.SmoothStep(0f, 1f, shiftElapsed / duration))
            : 0f;

        for (int row = 0; row < rows.Count; row++)
        {
            float y = topLeft.y - rowSpacing * (rows.Count - 1 - row) + offset;
            for (int column = 0; column < rows[row].Length; column++)
            {
                SpawnedBrick brick = rows[row][column];
                if (brick.brick != null)
                    brick.body.MovePosition(new Vector2(topLeft.x + columnSpacing * column, y));
            }
        }

        // Finish this slide before considering another already-cleared bottom row.
        if (isShifting && shiftElapsed >= duration)
            isShifting = false;
    }

    private Vector3 GetTopLeftPosition()
    {
        if (playAreaCamera == null)
            playAreaCamera = Camera.main;

        Vector3 position = topLeftPosition;
        if (playAreaCamera != null && playAreaCamera.orthographic)
            position.y = playAreaCamera.transform.position.y + playAreaCamera.orthographicSize - brickTopOffset;
        return position;
    }

    private SpawnedBrick[] SpawnRow(Vector3 leftPosition, bool allowEnemyBricks)
    {
        SpawnedBrick[] row = new SpawnedBrick[bricksPerRow];
        int specialsRemaining = allowEnemyBricks ? CurrentSpecialBricksPerRow : 0;
        if (specialsRemaining > 0 && (GameManager.Instance == null || GameManager.Instance.CurrentStage < 2) &&
            Random.value >= stageOneEnemyRowChance)
            specialsRemaining = 0;
        int livesRemaining = specialsRemaining < bricksPerRow && CanSpawnLifeBrick() ? 1 : 0;
        for (int column = 0; column < bricksPerRow; column++)
        {
            Vector3 position = leftPosition + Vector3.right * columnSpacing * column;
            bool special = specialsRemaining > 0 && Random.Range(0, bricksPerRow - column) < specialsRemaining;
            if (special)
                specialsRemaining--;
            bool life = !special && livesRemaining > 0 &&
                Random.Range(0, bricksPerRow - column - specialsRemaining) < livesRemaining;
            if (life)
                livesRemaining--;
            Brick selectedPrefab = special ? NextSpecialBrick() : life ? lifeBrickPrefab : brickPrefab;
            Brick brick = Instantiate(selectedPrefab, position, Quaternion.identity, transform);
            Rigidbody2D body = brick.GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            bool isRegular = selectedPrefab == brickPrefab;
            if (isRegular)
                ApplyStageAppearance(brick);
            row[column] = new SpawnedBrick { brick = brick, body = body };
        }
        return row;
    }

    private Brick NextSpecialBrick()
    {
        GameManager game = GameManager.Instance;
        if (game != null && game.CurrentStage >= 3 && dragonBrickPrefab != null)
            return dragonBrickPrefab;
        if (game == null || game.CurrentStage < 2)
            return specialBrickPrefab != null ? specialBrickPrefab : bombBrickPrefab;
        if (!game.BothEnemiesUnlocked)
            return bombBrickPrefab != null ? bombBrickPrefab : specialBrickPrefab;
        if (bombBrickPrefab == null)
            return specialBrickPrefab;
        if (specialBrickPrefab == null)
            return bombBrickPrefab;
        // From 150 points onward, alternate the two enemies within the existing special-brick budget.
        return specialSpawnCount++ % 2 == 0 ? bombBrickPrefab : specialBrickPrefab;
    }

    private void ApplyStageAppearance(Brick brick)
    {
        if (brick == null || GameManager.Instance == null)
            return;
        if (GameManager.Instance.CurrentStage >= 3 && stageThreeBrickIdle != null)
            brick.SetAppearance(stageThreeBrickIdle, stageThreeBrickBreakFrames);
        else if (GameManager.Instance.CurrentStage >= 2)
            brick.SetAppearance(stageTwoBrickIdle, stageTwoBrickBreakFrames);
    }

    private bool CanSpawnLifeBrick()
    {
        GameManager game = GameManager.Instance;
        if (lifeBrickPrefab == null || game == null || game.IsGameOver || game.Lives >= game.MaximumLives ||
            FindAnyObjectByType<LifeHeartPickup>() != null)
            return false;

        // An intact or breaking life brick already promises the next heart.
        foreach (Brick brick in FindObjectsByType<Brick>(FindObjectsSortMode.None))
        {
            if (brick.DropsLifeHeart)
                return false;
        }
        return true;
    }

    private static bool IsCleared(SpawnedBrick[] row)
    {
        foreach (SpawnedBrick brick in row)
        {
            if (brick.brick != null)
                return false;
        }
        return true;
    }
}
