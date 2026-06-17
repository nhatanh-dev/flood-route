using UnityEngine;

namespace Round1
{
    public sealed class Round1BoatWakeController : MonoBehaviour
    {
        [SerializeField] private Round1BoatController boatController;
        [SerializeField] private ParticleSystem wakeParticleSystem;

        private Round1BoatController subscribedBoatController;
        private bool isEmitting;

        private void Awake()
        {
            EnsureReferences();
            SubscribeToBoat();
            StopWake();
        }

        private void OnEnable()
        {
            EnsureReferences();
            SubscribeToBoat();
            StopWake();
        }

        private void OnDisable()
        {
            UnsubscribeFromBoat();
            StopWake();
        }

        private void Update()
        {
            EnsureReferences();
            SubscribeToBoat();

            bool shouldEmit = boatController != null && boatController.IsMoving;
            if (shouldEmit && !isEmitting)
            {
                StartWake();
                return;
            }

            if (!shouldEmit && isEmitting)
            {
                StopWake();
            }
        }

        private void HandleBoatMovementStarted()
        {
            StartWake();
        }

        private void HandleBoatArrived(Round1NodeId nodeId)
        {
            StopWake();
        }

        private void StartWake()
        {
            if (wakeParticleSystem == null)
            {
                return;
            }

            ParticleSystem.EmissionModule emission = wakeParticleSystem.emission;
            emission.enabled = true;
            wakeParticleSystem.Play(false);

            isEmitting = true;
        }

        private void StopWake()
        {
            if (wakeParticleSystem == null)
            {
                isEmitting = false;
                return;
            }

            ParticleSystem.EmissionModule emission = wakeParticleSystem.emission;
            emission.enabled = false;
            wakeParticleSystem.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            isEmitting = false;
        }

        private void EnsureReferences()
        {
            boatController ??= FindAnyObjectByType<Round1BoatController>();
            wakeParticleSystem ??= GetComponent<ParticleSystem>();
        }

        private void SubscribeToBoat()
        {
            if (boatController == null || subscribedBoatController == boatController)
            {
                return;
            }

            UnsubscribeFromBoat();
            subscribedBoatController = boatController;
            subscribedBoatController.MovementStarted += HandleBoatMovementStarted;
            subscribedBoatController.ArrivedAtNode += HandleBoatArrived;
        }

        private void UnsubscribeFromBoat()
        {
            if (subscribedBoatController == null)
            {
                return;
            }

            subscribedBoatController.MovementStarted -= HandleBoatMovementStarted;
            subscribedBoatController.ArrivedAtNode -= HandleBoatArrived;
            subscribedBoatController = null;
        }
    }
}
