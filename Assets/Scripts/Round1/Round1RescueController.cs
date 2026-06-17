using System;
using UnityEngine;

namespace Round1
{
    public sealed class Round1RescueController : MonoBehaviour
    {
        [SerializeField] private Round1SceneReferences sceneReferences;
        [SerializeField] private Round1BoatController boatController;
        [SerializeField] private int cargoCapacity = 3;

        private const int InitialNhaBaCivilians = 2;
        private const int InitialNhaTuCivilians = 1;
        private const int BaiDinhCapacity = 3;
        private const int GoCaoCapacity = 2;

        private Round1BoatController subscribedBoatController;

        public int Cargo { get; private set; }
        public int CargoCapacity => cargoCapacity;
        public int Saved { get; private set; }
        public int TotalCivilians => InitialNhaBaCivilians + InitialNhaTuCivilians;
        public int RemainingAtNhaBa { get; private set; } = InitialNhaBaCivilians;
        public int RemainingAtNhaTu { get; private set; } = InitialNhaTuCivilians;
        public int SavedAtBaiDinh { get; private set; }
        public int SavedAtGoCao { get; private set; }

        public event Action RescueStateChanged;

        private void Awake()
        {
            EnsureReferences();
            ResetRescueState();
        }

        private void OnEnable()
        {
            EnsureReferences();
            SubscribeToBoat();
        }

        private void OnDisable()
        {
            UnsubscribeFromBoat();
        }

        public void ResetRescueState()
        {
            Cargo = 0;
            Saved = 0;
            RemainingAtNhaBa = InitialNhaBaCivilians;
            RemainingAtNhaTu = InitialNhaTuCivilians;
            SavedAtBaiDinh = 0;
            SavedAtGoCao = 0;

            SetCivilianVisualActive(sceneReferences != null ? sceneReferences.civilianR1NhaBa1 : null, true);
            SetCivilianVisualActive(sceneReferences != null ? sceneReferences.civilianR1NhaBa2 : null, true);
            SetCivilianVisualActive(sceneReferences != null ? sceneReferences.civilianR1NhaTu1 : null, true);

            RescueStateChanged?.Invoke();
        }

        [ContextMenu("Reset Rescue State")]
        private void ResetRescueStateContext()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            ResetRescueState();
        }

        [ContextMenu("Test NhaBa Pickup")]
        private void TestNhaBaPickup()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            ResetRescueState();
            HandleArrivalAtNode(Round1NodeId.NhaBa);
        }

        [ContextMenu("Test NhaTu Pickup")]
        private void TestNhaTuPickup()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            ResetRescueState();
            Cargo = 2;
            HandleArrivalAtNode(Round1NodeId.NhaTu);
        }

        [ContextMenu("Test BaiDinh Drop-Off")]
        private void TestBaiDinhDropOff()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            ResetRescueState();
            Cargo = 3;
            HandleArrivalAtNode(Round1NodeId.BaiDinh);
        }

        [ContextMenu("Test GoCao Capacity")]
        private void TestGoCaoCapacity()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            ResetRescueState();
            Cargo = 3;
            HandleArrivalAtNode(Round1NodeId.GoCao);
        }

        private void HandleArrivalAtNode(Round1NodeId nodeId)
        {
            bool changed = false;
            changed |= TryPickupAt(nodeId);
            changed |= TryDropOffAt(nodeId);

            if (changed)
            {
                RescueStateChanged?.Invoke();
            }
        }

        private bool TryPickupAt(Round1NodeId nodeId)
        {
            if (nodeId == Round1NodeId.NhaBa)
            {
                return PickupFromNhaBa();
            }

            if (nodeId == Round1NodeId.NhaTu)
            {
                return PickupFromNhaTu();
            }

            return false;
        }

        private bool PickupFromNhaBa()
        {
            int availableCargoSlots = Mathf.Max(0, CargoCapacity - Cargo);
            int pickupCount = Mathf.Min(RemainingAtNhaBa, availableCargoSlots);
            if (pickupCount <= 0)
            {
                return false;
            }

            for (int i = 0; i < pickupCount; i++)
            {
                int civilianIndex = InitialNhaBaCivilians - RemainingAtNhaBa;
                RemainingAtNhaBa -= 1;
                Cargo += 1;
                DisableNhaBaCivilianVisual(civilianIndex);
            }

            return true;
        }

        private bool PickupFromNhaTu()
        {
            if (RemainingAtNhaTu <= 0 || Cargo >= CargoCapacity)
            {
                return false;
            }

            RemainingAtNhaTu = 0;
            Cargo += 1;
            SetCivilianVisualActive(sceneReferences != null ? sceneReferences.civilianR1NhaTu1 : null, false);
            return true;
        }

        private bool TryDropOffAt(Round1NodeId nodeId)
        {
            if (nodeId == Round1NodeId.BaiDinh)
            {
                return DropOffAtBaiDinh();
            }

            if (nodeId == Round1NodeId.GoCao)
            {
                return DropOffAtGoCao();
            }

            return false;
        }

        private bool DropOffAtBaiDinh()
        {
            int destinationSlots = Mathf.Max(0, BaiDinhCapacity - SavedAtBaiDinh);
            int dropCount = Mathf.Min(Cargo, destinationSlots);
            if (dropCount <= 0)
            {
                return false;
            }

            Cargo -= dropCount;
            Saved += dropCount;
            SavedAtBaiDinh += dropCount;
            return true;
        }

        private bool DropOffAtGoCao()
        {
            int destinationSlots = Mathf.Max(0, GoCaoCapacity - SavedAtGoCao);
            int dropCount = Mathf.Min(Cargo, destinationSlots);
            if (dropCount <= 0)
            {
                return false;
            }

            Cargo -= dropCount;
            Saved += dropCount;
            SavedAtGoCao += dropCount;
            return true;
        }

        private void DisableNhaBaCivilianVisual(int civilianIndex)
        {
            Transform civilianVisual = civilianIndex == 0
                ? sceneReferences != null ? sceneReferences.civilianR1NhaBa1 : null
                : sceneReferences != null ? sceneReferences.civilianR1NhaBa2 : null;

            SetCivilianVisualActive(civilianVisual, false);
        }

        private static void SetCivilianVisualActive(Transform civilianVisual, bool active)
        {
            if (civilianVisual == null)
            {
                return;
            }

            civilianVisual.gameObject.SetActive(active);
        }

        private void EnsureReferences()
        {
            sceneReferences ??= FindAnyObjectByType<Round1SceneReferences>();
            boatController ??= FindAnyObjectByType<Round1BoatController>();
        }

        private void SubscribeToBoat()
        {
            if (boatController == null || subscribedBoatController == boatController)
            {
                return;
            }

            UnsubscribeFromBoat();
            subscribedBoatController = boatController;
            subscribedBoatController.ArrivedAtNode += HandleArrivalAtNode;
        }

        private void UnsubscribeFromBoat()
        {
            if (subscribedBoatController == null)
            {
                return;
            }

            subscribedBoatController.ArrivedAtNode -= HandleArrivalAtNode;
            subscribedBoatController = null;
        }
    }
}
