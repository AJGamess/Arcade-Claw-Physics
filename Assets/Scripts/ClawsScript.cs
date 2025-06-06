using UnityEngine;
using System.Collections;

internal enum CraneState
{
    Controllable,
    Dropping,
    ReturningUp,
    MovingLeft,
    DroppingOff,
    ReturningHome,
    GameOver
}

[RequireComponent(typeof(Rigidbody))]
public class ClawsScript : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float minX = -5f;
    public float maxX = 5f;
    public Transform dropStartPos;
    public Transform dropEndPos;
    public Transform returnLeftPos;
    public Transform homePos;

    public float dropSpeed = 2f;
    public float returnSpeed = 5f;

    public Animator clawAnimator;

    public float liftHeight = 16f;

    private CraneState currentState = CraneState.Controllable;
    private bool hasBlock = false;
    private GameObject grabbedBlock;
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
    }

    private void Update()
    {
        switch (currentState)
        {
            case CraneState.Controllable:
                HandleMovement();
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    currentState = CraneState.Dropping;
                    StartCoroutine(DropClaw());
                }
                break;

            case CraneState.ReturningUp:
                MoveVerticalTo(18f, () => currentState = CraneState.MovingLeft); // Go higher than wall
                break;

            case CraneState.MovingLeft:
                MoveToX(returnLeftPos.position.x, () => currentState = CraneState.DroppingOff);
                break;

            case CraneState.DroppingOff:
                StartCoroutine(PlayOpenAndDropBlock());
                break;

            case CraneState.ReturningHome:
                MoveTo(homePos.position, () =>
                {
                    if (clawAnimator != null)
                    {
                        clawAnimator.SetTrigger("Idle");
                    }
                    currentState = CraneState.Controllable;
                });
                break;
        }
    }

    private IEnumerator PlayOpenAndDropBlock()
    {
        // Trigger open animation first
        clawAnimator?.SetTrigger("Open");

        yield return new WaitForSeconds(0.5f); // Let open animation play a bit

        // Drop the block visually/physically
        DropBlock();

        yield return new WaitForSeconds(0.5f); // Pause before returning

        currentState = CraneState.ReturningHome;
    }
    private void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float newX = Mathf.Clamp(rb.position.x + horizontal * moveSpeed * Time.deltaTime, minX, maxX);
        Vector3 target = new Vector3(newX, rb.position.y, rb.position.z);
        rb.MovePosition(target);
    }

    private IEnumerator DropClaw()
    {
        // Trigger open animation
        clawAnimator?.SetTrigger("Open");

        float t = 0;
        float startY = dropStartPos.position.y;
        float endY = dropEndPos.position.y;

        // Drop down
        while (t < 1f)
        {
            t += Time.deltaTime * dropSpeed;
            float y = Mathf.Lerp(startY, endY, t);
            rb.MovePosition(new Vector3(rb.position.x, y, rb.position.z));
            yield return null;
        }

        // Trigger close animation
        clawAnimator?.SetTrigger("Close");

        // Try to detect block
        Collider[] hits = Physics.OverlapSphere(transform.position, 0.5f);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Points"))
            {
                hasBlock = true;
                grabbedBlock = hit.gameObject;
                grabbedBlock.transform.SetParent(transform);
                break;
            }
        }

        // Wait before rising
        yield return new WaitForSeconds(0.5f);

        // Return up
        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * dropSpeed;
            float y = Mathf.Lerp(endY, startY, t);
            rb.MovePosition(new Vector3(rb.position.x, y, rb.position.z));
            yield return null;
        }

        currentState = CraneState.ReturningUp;
    }

    private void DropBlock()
    {
        if (hasBlock && grabbedBlock != null)
        {
            grabbedBlock.transform.SetParent(null);
            Rigidbody blockRb = grabbedBlock.GetComponent<Rigidbody>();
            if (blockRb != null)
            {
                blockRb.useGravity = true;
                blockRb.linearVelocity = Vector3.zero;
            }
            hasBlock = false;
            grabbedBlock = null;
        }
    }

    private void MoveToX(float targetX, System.Action onArrive)
    {
        Vector3 target = new Vector3(targetX, rb.position.y, rb.position.z);
        rb.MovePosition(Vector3.MoveTowards(rb.position, target, returnSpeed * Time.deltaTime));
        if (Mathf.Abs(rb.position.x - targetX) < 0.05f)
        {
            onArrive?.Invoke();
        }
    }

    private void MoveVerticalTo(float targetY, System.Action onArrive)
    {
        Vector3 target = new Vector3(rb.position.x, targetY, rb.position.z);
        rb.MovePosition(Vector3.MoveTowards(rb.position, target, returnSpeed * Time.deltaTime));
        if (Mathf.Abs(rb.position.y - targetY) < 0.05f)
        {
            onArrive?.Invoke();
        }
    }

    private void MoveTo(Vector3 target, System.Action onArrive)
    {
        rb.MovePosition(Vector3.MoveTowards(rb.position, target, returnSpeed * Time.deltaTime));
        if (Vector3.Distance(rb.position, target) < 0.1f)
        {
            onArrive?.Invoke();
        }
    }
}
