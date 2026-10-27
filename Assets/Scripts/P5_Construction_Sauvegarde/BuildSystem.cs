using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// P5 - Mode construction : un apercu (fantome) se place devant le joueur sur une grille,
// on le tourne puis on le pose. Verifie aussi si le joueur est a l'abri (sous un toit) ou pres d'un feu.
public class BuildSystem : MonoBehaviour
{
    public static BuildSystem Instance { get; private set; }

    [Tooltip("Un prefab par type, dans l'ordre : Sol, Mur, Porte, Toit, Feu de camp")]
    public BuildPiece[] prefabs;
    public Transform player;
    public Inventory inventory;
    public PlayerStats stats;
    public Material ghostValid;
    public Material ghostInvalid;
    public float gridSize = 3f;
    public float placeDistance = 3.5f;
    public float fireRadius = 6f;

    public readonly List<BuildPiece> placed = new List<BuildPiece>();

    public bool IsPlacing => ghost != null;
    public BuildPieceType Current { get; private set; }
    public bool CanPlace { get; private set; }

    GameObject ghost;
    Renderer[] ghostRenderers;
    float extraRotation;
    float nextShelterCheck;

    void Awake() => Instance = this;

    public void BeginPlacement(BuildPieceType type)
    {
        CancelPlacement();
        Current = type;
        extraRotation = 0f;

        ghost = Instantiate(prefabs[(int)type].gameObject);
        ghost.name = "Ghost";
        foreach (var c in ghost.GetComponentsInChildren<Collider>()) Destroy(c);
        foreach (var o in ghost.GetComponentsInChildren<NavMeshObstacle>()) Destroy(o);
        foreach (var l in ghost.GetComponentsInChildren<Light>()) Destroy(l);
        foreach (var m in ghost.GetComponentsInChildren<MonoBehaviour>()) Destroy(m);
        ghostRenderers = ghost.GetComponentsInChildren<Renderer>();
    }

    public void Rotate() => extraRotation += 90f;

    public void CancelPlacement()
    {
        if (ghost) Destroy(ghost);
        ghost = null;
    }

    public bool ConfirmPlacement()
    {
        if (!ghost) return false;
        var cost = BuildPiece.Cost(Current);
        if (!inventory.Has(cost))
        {
            UIRoot.Toast("Il faut : " + ItemDB.CostText(cost));
            return false;
        }
        if (!CanPlace)
        {
            UIRoot.Toast("Impossible de construire ici.");
            return false;
        }
        inventory.Spend(cost);
        Place(Current, ghost.transform.position, ghost.transform.eulerAngles.y);
        return true;
    }

    public BuildPiece Place(BuildPieceType type, Vector3 position, float yRotation)
    {
        var piece = Instantiate(prefabs[(int)type], position, Quaternion.Euler(0f, yRotation, 0f));
        placed.Add(piece);
        return piece;
    }

    public void ClearAll()
    {
        CancelPlacement();
        foreach (var p in placed) if (p) Destroy(p.gameObject);
        placed.Clear();
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        if (Time.time >= nextShelterCheck)
        {
            nextShelterCheck = Time.time + 0.25f;
            stats.Sheltered = IsUnderRoof(player.position);
            stats.NearFire = IsNearFire(player.position);
        }

        if (ghost) UpdateGhost();
    }

    void UpdateGhost()
    {
        float yaw = Mathf.Round(player.eulerAngles.y / 90f) * 90f + extraRotation;
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        Vector3 p = player.position + player.forward * placeDistance;

        if (Current == BuildPieceType.Campfire)
        {
            p = new Vector3(Mathf.Round(p.x), 0f, Mathf.Round(p.z));
        }
        else
        {
            p = new Vector3(Mathf.Round(p.x / gridSize) * gridSize, 0f, Mathf.Round(p.z / gridSize) * gridSize);
            if (Current == BuildPieceType.Wall || Current == BuildPieceType.Doorway)
                p += rot * new Vector3(0f, 0f, gridSize * 0.5f);
        }
        ghost.transform.SetPositionAndRotation(p, rot);

        CanPlace = !IsOccupied(Current, p);
        bool affordable = inventory.Has(BuildPiece.Cost(Current));
        var mat = CanPlace && affordable ? ghostValid : ghostInvalid;
        foreach (var r in ghostRenderers)
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            r.sharedMaterials = mats;
        }
    }

    bool IsOccupied(BuildPieceType type, Vector3 p)
    {
        bool isWall = type == BuildPieceType.Wall || type == BuildPieceType.Doorway;
        foreach (var piece in placed)
        {
            if (!piece) continue;
            bool otherWall = piece.type == BuildPieceType.Wall || piece.type == BuildPieceType.Doorway;
            bool sameKind = isWall ? otherWall : piece.type == type;
            float minDist = type == BuildPieceType.Campfire ? 1.5f : 0.2f;
            if (sameKind && Vector3.Distance(piece.transform.position, p) < minDist) return true;
        }
        return false;
    }

    public bool IsUnderRoof(Vector3 position)
    {
        if (!Physics.Raycast(position + Vector3.up, Vector3.up, out RaycastHit hit, 6f, ~(1 << 2), QueryTriggerInteraction.Ignore))
            return false;
        var piece = hit.collider.GetComponentInParent<BuildPiece>();
        return piece && piece.type == BuildPieceType.Roof;
    }

    public bool IsNearFire(Vector3 position)
    {
        foreach (var piece in placed)
            if (piece && piece.type == BuildPieceType.Campfire && Vector3.Distance(piece.transform.position, position) < fireRadius)
                return true;
        return false;
    }

    // Pour la boussole (P3) : le toit le plus proche, sinon le feu de camp, sinon n'importe quelle piece.
    public bool TryGetNearestShelter(Vector3 from, out Vector3 position)
    {
        position = Vector3.zero;
        int bestRank = int.MaxValue;
        float bestDist = float.MaxValue;
        foreach (var piece in placed)
        {
            if (!piece) continue;
            int rank = piece.type == BuildPieceType.Roof ? 0 : piece.type == BuildPieceType.Campfire ? 1 : 2;
            float d = Vector3.Distance(from, piece.transform.position);
            if (rank < bestRank || (rank == bestRank && d < bestDist))
            {
                bestRank = rank;
                bestDist = d;
                position = piece.transform.position;
            }
        }
        return bestRank != int.MaxValue;
    }
}
