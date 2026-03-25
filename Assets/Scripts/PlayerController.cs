using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // Rigidbody of the player.
    private Rigidbody rb; 

    // Variable to keep track of collected "PickUp" objects.
    private int count;

    // Movement along X and Y axes.
    private float movementX;
    private float movementY;

    // Speed at which the player moves.
    public float speed = 0;

    // set if is AI controlled
    public bool isComputerControlled = false;

    // Shared minigame state (victory ends the whole game).
    private static bool _gameEnded = false;
    private static int _winThreshold = -1;
    private static int _totalPickups = -1;
    private static readonly List<PlayerController> _instances = new List<PlayerController>();

    private string _playerLabel = "";

    // Human control mapping: Player1 = WASD, Player2 = arrows (in this scene they are named accordingly).
    private enum ControlScheme { WASD, Arrows }
    private ControlScheme _controlScheme = ControlScheme.WASD;

    // Simple AI state.
    private float _aiNextDecisionTime = 0f;
    private Transform _aiCurrentTarget = null;

    // UI text component to display count of "PickUp" objects collected.
    public TextMeshProUGUI countText;

    // UI object to display winning text.
    public GameObject winTextObject;
    

    // Start is called before the first frame update.
    void Start()
    {
        _instances.Add(this);

        // Get and store the Rigidbody component attached to the player.
        rb = GetComponent<Rigidbody>();

        // Initialize count to zero.
        count = 0;

        // Resolve which keys to use for human control.
        // In the scene, players are named "Player1" and "Player2".
        if (gameObject.name == "Player2")
            _controlScheme = ControlScheme.Arrows;
        else
            _controlScheme = ControlScheme.WASD;

        _playerLabel = gameObject.name;

        // Initialize shared win threshold once.
        if (_winThreshold < 0)
        {
            _totalPickups = GameObject.FindGameObjectsWithTag("PickUp").Length;
            _winThreshold = (_totalPickups / 2) + 1; // strictly more than half
        }

        // Initially set the win text to be inactive.
        if (winTextObject != null)
            winTextObject.SetActive(false);

        // Update the count display.
        SetCountText();
    }

    private void OnDestroy()
    {
        _instances.Remove(this);
    }

    private void Update()
    {
        if (_gameEnded)
        {
            movementX = 0;
            movementY = 0;
            return;
        }

        if (isComputerControlled)
            UpdateAIInput();
        else
            UpdateKeyboardInput();
    }

    private void UpdateKeyboardInput()
    {
        // Keyboard mapping: Player1 uses WASD, Player2 uses arrows.
        Vector2 input = Vector2.zero;
        var keyboard = Keyboard.current;
        if (keyboard == null) { movementX = 0; movementY = 0; return; }

        if (_controlScheme == ControlScheme.WASD)
        {
            if (keyboard.wKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed) input.y -= 1f;
            if (keyboard.aKey.isPressed) input.x -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
        }
        else
        {
            if (keyboard.upArrowKey.isPressed) input.y += 1f;
            if (keyboard.downArrowKey.isPressed) input.y -= 1f;
            if (keyboard.leftArrowKey.isPressed) input.x -= 1f;
            if (keyboard.rightArrowKey.isPressed) input.x += 1f;
        }

        if (input.sqrMagnitude > 1f)
            input.Normalize();

        // movementX maps to Rigidbody X movement, movementY maps to Rigidbody Z movement.
        movementX = input.x;
        movementY = input.y;
    }

    private void UpdateAIInput()
    {
        // Simple AI: move towards the nearest active PickUp.
        if (Time.time < _aiNextDecisionTime)
            return;
        _aiNextDecisionTime = Time.time + 0.15f;

        if (_aiCurrentTarget == null || !_aiCurrentTarget.gameObject.activeInHierarchy)
            _aiCurrentTarget = FindNearestPickupTarget();

        if (_aiCurrentTarget == null)
        {
            movementX = 0;
            movementY = 0;
            return;
        }

        Vector3 toTarget = _aiCurrentTarget.position - transform.position;
        toTarget.y = 0f; // XZ plane only.

        if (toTarget.sqrMagnitude < 0.0001f)
        {
            movementX = 0;
            movementY = 0;
            return;
        }

        Vector3 dir = toTarget.normalized;
        movementX = dir.x;
        movementY = dir.z;
    }

    private Transform FindNearestPickupTarget()
    {
        GameObject[] pickups = GameObject.FindGameObjectsWithTag("PickUp");
        Transform best = null;
        float bestSqrDist = float.PositiveInfinity;

        foreach (var p in pickups)
        {
            if (p == null || !p.activeInHierarchy) continue;
            float sqr = (p.transform.position - transform.position).sqrMagnitude;
            if (sqr < bestSqrDist)
            {
                bestSqrDist = sqr;
                best = p.transform;
            }
        }

        return best;
    }

    private void StopAllPlayers()
    {
        foreach (var instance in _instances)
        {
            if (instance == null) continue;
            instance.movementX = 0;
            instance.movementY = 0;
            if (instance.rb != null)
            {
                instance.rb.linearVelocity = Vector3.zero;
                instance.rb.angularVelocity = Vector3.zero;
                instance.rb.isKinematic = true; // Stop physics-based movement.
            }
            instance.enabled = false; // Prevent any more triggers from being processed.
        }
    }

    private void EndGame(string winnerLabel)
    {
        if (_gameEnded) return;
        _gameEnded = true;

        if (winTextObject != null && gameObject.name == winnerLabel)
        {
            winTextObject.SetActive(true);
            var tmp = winTextObject.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = $"{winnerLabel} Wins!";
        }

        StopAllPlayers();
    }
 
    // This function is called when a move input is detected.
    void OnMove(InputValue movementValue)
    {
        // Human control is handled via direct keyboard polling.
        // We keep this method only because the PlayerInput component may send messages.
    }

    // FixedUpdate is called once per fixed frame-rate frame.
    private void FixedUpdate() 
    {
        // Create a 3D movement vector using the X and Y inputs.
        Vector3 movement = new Vector3 (movementX, 0.0f, movementY);

        // Apply force to the Rigidbody to move the player.
        rb.AddForce(movement * speed); 
    }

 
    void OnTriggerEnter(Collider other) 
    {
        if (_gameEnded) return;

        // Check if the object the player collided with has the "PickUp" tag.
        if (other.gameObject.CompareTag("PickUp")) 
        {
            // Deactivate the collided object (making it disappear).
            other.gameObject.SetActive(false);

            // Increment the count of "PickUp" objects collected.
            count = count + 1;

            // Update the count display.
            SetCountText();
        }
    }

    // Function to update the displayed count of "PickUp" objects collected.
    void SetCountText() 
    {
        // Update the count text with the current count.
        if (countText != null)
            countText.text = "Count: " + count.ToString();

        // Check if the count has reached or exceeded the win condition.
        if (!_gameEnded && _winThreshold >= 0 && count >= _winThreshold)
            EndGame(_playerLabel);
    }

    // Legacy gameplay "defeat by Enemy collision" removed.
}
