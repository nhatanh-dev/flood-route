using System.Collections;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Round1
{
    /// <summary>
    /// Handles all first-person E-key rescue/dropoff interaction for Round1_FirstPersonPrototype.
    /// Runs on the FP_CameraRig object so it moves with the boat.
    /// Owns all HUD updates in the FP scene (HudController is auto-disabled).
    /// </summary>
    public class Round1FirstPersonInteraction : MonoBehaviour
    {
        [Header("Detection")]
        public float interactRadius = 6f;

        // ── cached refs ───────────────────────────────────────────────────────
        private Round1RescueController rescueController;
        private R1RealtimeRoundController realtimeController;
        private Round1DebrisController debrisController;
        private Round1SceneReferences  refs;
        private R1RouteGraph           routeGraph;

        // ── HUD – shared named TMP objects ────────────────────────────────────
        private TMP_Text txtTurn;       // TXT_R1_Shared_Turn (now Timer)
        private TMP_Text txtCargo;      // TXT_R1_Shared_Cargo
        private TMP_Text txtSaved;      // TXT_R1_Shared_Saved
        private TMP_Text txtObjective;  // TXT_R1_Shared_Objective
        private TMP_Text txtMessage;    // TXT_R1_Shared_Message (bottom bar)
        private TMP_Text txtDurability; // TXT_R1_Shared_Durability

        // ── node trigger transforms ───────────────────────────────────────────
        private Transform tNhaBa;
        private Transform tNhaTu;
        private Transform tBaiDinh;
        private Transform tGoCao;
        private Transform tBenPhu;

        // ── badge root objects ────────────────────────────────────────────────
        private GameObject nhaBaBadgeRoot;
        private GameObject nhaTuBadgeRoot;
        private TMP_Text   nhaBaBadgeText;
        private TMP_Text   nhaTuBadgeText;

        // ── state ─────────────────────────────────────────────────────────────
        private bool gameOver;

        // ─────────────────────────────────────────────────────────────────────
        void Start()
        {
            rescueController = FindAnyObjectByType<Round1RescueController>();
            realtimeController = FindAnyObjectByType<R1RealtimeRoundController>();
            debrisController = FindAnyObjectByType<Round1DebrisController>();
            refs             = FindAnyObjectByType<Round1SceneReferences>();

            if (refs != null)
            {
                tNhaBa   = refs.r1NhaBa;
                tNhaTu   = refs.r1NhaTu;
                tBaiDinh = refs.r1BaiDinh;
                tGoCao   = refs.r1GoCao;
                tBenPhu  = refs.r1BenPhu;
            }

            // Locate the shared named HUD objects
            txtTurn      = FindTMP("TXT_R1_Shared_Turn");
            txtCargo     = FindTMP("TXT_R1_Shared_Cargo");
            txtSaved     = FindTMP("TXT_R1_Shared_Saved");
            txtObjective = FindTMP("TXT_R1_Shared_Objective");
            txtMessage   = FindTMP("TXT_R1_Shared_Message");

            // Setup Durability text
            txtDurability = FindTMP("TXT_R1_Shared_Durability");
            if (txtDurability == null && txtTurn != null)
            {
                var durGo = Instantiate(txtTurn.gameObject, txtTurn.transform.parent);
                durGo.name = "TXT_R1_Shared_Durability";
                durGo.transform.position -= new Vector3(0, 40, 0); // shift down
                txtDurability = durGo.GetComponent<TMP_Text>();
            }

            // Badge roots
            nhaBaBadgeRoot = GameObject.Find("R1_RescueBadge_NhaBa_2Nguoi");
            nhaTuBadgeRoot = GameObject.Find("R1_RescueBadge_NhaTu_1Nguoi");

            // Get the count-label TMP inside each badge (first TMP_Text that shows a number)
            if (nhaBaBadgeRoot != null)
                nhaBaBadgeText = FindBadgeCountText(nhaBaBadgeRoot);
            if (nhaTuBadgeRoot != null)
                nhaTuBadgeText = FindBadgeCountText(nhaTuBadgeRoot);

            // Set bottom instruction bar
            SetMsg("WASD: Lái thuyền | E: Cứu/Thả | Tab: Bản đồ");

            // Turn counter is now used in FP mode
            // if (txtTurn != null) txtTurn.text = "Lượt: —";

            // Initial HUD refresh
            RefreshHUD();
        }

        // ─────────────────────────────────────────────────────────────────────
        void Update()
        {
            if (gameOver || rescueController == null) return;
            
            var keyboard = Keyboard.current;
            
            if (realtimeController != null && realtimeController.IsGameOver)
            {
                if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
                {
                    Time.timeScale = 1f;
                    UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
                }
                return;
            }

            // ── proximity detection ───────────────────────────────────────────
            Vector3 pos = transform.position;
            bool nearNhaBa   = tNhaBa   != null && Vector3.Distance(pos, tNhaBa.position)   < interactRadius;
            bool nearNhaTu   = tNhaTu   != null && Vector3.Distance(pos, tNhaTu.position)   < interactRadius;
            bool nearShelter = (tBaiDinh != null && Vector3.Distance(pos, tBaiDinh.position) < interactRadius)
                            || (tGoCao   != null && Vector3.Distance(pos, tGoCao.position)   < interactRadius);
            bool nearWait    = tBenPhu  != null && Vector3.Distance(pos, tBenPhu.position)   < interactRadius;

            // ── input ─────────────────────────────────────────────────────────
            bool ePressed = keyboard != null && keyboard.eKey.wasPressedThisFrame;
            bool qPressed = keyboard != null && keyboard.qKey.wasPressedThisFrame;

            // ── build objective prompt & handle E ─────────────────────────────
            string prompt = "";

            float boatSpeed = 0f;
            var boatCtrl = GetComponent<Round1FirstPersonBoatController>();
            if (boatCtrl != null) boatSpeed = Mathf.Abs(boatCtrl.currentSpeed);
            float maxInteractSpeed = 1.2f;

            int cargo = realtimeController != null ? realtimeController.currentCargo : 0;
            int capacity = realtimeController != null ? realtimeController.boatCapacity : 3;
            string objText = realtimeController != null ? realtimeController.currentObjectiveText : "";

            if (nearNhaBa && objText.Contains("Nhà cần cứu 1"))
            {
                if (cargo < capacity)
                {
                    if (boatSpeed <= maxInteractSpeed)
                    {
                        prompt = "Nhấn E để cứu 2 người.";
                        // if (ePressed) DoPickupNhaBa(); // disabled for now as requested
                    }
                    else
                    {
                        prompt = "Giảm tốc để tiếp cận an toàn.";
                    }
                }
            }
            else if (nearNhaTu && objText.Contains("Nhà cần cứu 2"))
            {
                if (cargo < capacity)
                {
                    if (boatSpeed <= maxInteractSpeed)
                    {
                        prompt = "Nhấn E để cứu 1 người.";
                        // if (ePressed) DoPickupNhaTu(); // disabled for now as requested
                    }
                    else
                    {
                        prompt = "Giảm tốc để tiếp cận an toàn.";
                    }
                }
            }
            else if (nearShelter)
            {
                if (cargo > 0 && objText.Contains("Điểm trú"))
                {
                    if (boatSpeed <= maxInteractSpeed)
                    {
                        prompt = "Nhấn E để đưa người dân vào điểm trú.";
                        // if (ePressed) DoDropOff(); // disabled for now as requested
                    }
                    else
                    {
                        prompt = "Giảm tốc để cập bến an toàn.";
                    }
                }
                else if (cargo == 0)
                {
                    prompt = "Chưa có người dân trên thuyền.";
                }
            }

            // ── handle Q ─────────────────────────────────────────────────────
            // Q logic is now handled by R1RouteTargetController
            /*
            if (qPressed && turnController != null)
            {
                if (turnController.TryRequestWait())
                    ShowFeedback(nearWait ? "Đã chờ nước cuốn rác trôi." : "Đã chờ 1 lượt.");
            }
            */

            // ── keep HUD in sync every frame ──────────────────────────────────
            RefreshHUD();

            // ── update objective text every frame ─────────────────────────────
            SetObjective(prompt);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Rescue logic
        // ─────────────────────────────────────────────────────────────────────
        private void DoPickupNhaBa()
        {
            var method = typeof(Round1RescueController).GetMethod(
                "PickupFromNhaBa", BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null) { Debug.LogError("[FP] PickupFromNhaBa not found"); return; }

            bool ok = (bool)method.Invoke(rescueController, null);
            if (!ok) return;

            // Badge → "Đã đón"
            if (rescueController.RemainingAtNhaBa <= 0 && nhaBaBadgeText != null)
                nhaBaBadgeText.text = "Đã đón";

            RefreshHUD();
            CheckWin();
        }

        private void DoPickupNhaTu()
        {
            var method = typeof(Round1RescueController).GetMethod(
                "PickupFromNhaTu", BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null) { Debug.LogError("[FP] PickupFromNhaTu not found"); return; }

            bool ok = (bool)method.Invoke(rescueController, null);
            if (!ok) return;

            // Badge → "Đã đón"
            if (rescueController.RemainingAtNhaTu <= 0 && nhaTuBadgeText != null)
                nhaTuBadgeText.text = "Đã đón";

            RefreshHUD();
            CheckWin();
        }

        private void DoDropOff()
        {
            var m1 = typeof(Round1RescueController).GetMethod(
                "DropOffAtBaiDinh", BindingFlags.NonPublic | BindingFlags.Instance);
            var m2 = typeof(Round1RescueController).GetMethod(
                "DropOffAtGoCao", BindingFlags.NonPublic | BindingFlags.Instance);

            bool any = false;
            if (m1 != null) any |= (bool)m1.Invoke(rescueController, null);
            // Only try GoCao if Cargo remains (BaiDinh may not have taken all)
            if (m2 != null && rescueController.Cargo > 0)
                any |= (bool)m2.Invoke(rescueController, null);

            if (!any) return;

            RefreshHUD();
            CheckWin();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  HUD helpers
        // ─────────────────────────────────────────────────────────────────────
        private void RefreshHUD()
        {
            if (realtimeController != null)
            {
                if (txtTurn != null)
                {
                    int mins = Mathf.FloorToInt(realtimeController.currentTimeRemaining / 60F);
                    int secs = Mathf.FloorToInt(realtimeController.currentTimeRemaining - mins * 60);
                    txtTurn.text = string.Format("Thời gian: {0:00}:{1:00}", mins, secs);
                }

                if (txtDurability != null)
                {
                    txtDurability.text = $"Độ bền thuyền: {realtimeController.currentBoatDurability}/{realtimeController.maxBoatDurability}";
                }

                if (txtCargo != null)
                    txtCargo.text = $"Trên thuyền: {realtimeController.currentCargo}/{realtimeController.boatCapacity}";
                
                if (txtSaved != null)
                    txtSaved.text = $"Đã an toàn: {realtimeController.civiliansSafe}/{realtimeController.totalCivilians}";
            }
        }

        public void HideGameplayHUD()
        {
            if (txtTurn != null) txtTurn.transform.parent.gameObject.SetActive(false);
            if (txtMessage != null) txtMessage.gameObject.SetActive(false);
        }

        private void SetObjective(string interactPrompt)
        {
            if (txtObjective == null) return;

            if (!string.IsNullOrEmpty(interactPrompt))
            {
                txtObjective.text = interactPrompt;
                return;
            }

            if (realtimeController != null)
            {
                txtObjective.text = "Mục tiêu: " + realtimeController.currentObjectiveText;
            }
        }

        private void SetMsg(string msg)
        {
            if (txtMessage != null) txtMessage.text = msg;
        }

        private void CheckWin()
        {
            if (rescueController.Saved < rescueController.TotalCivilians) return;

            gameOver = true;

            if (refs != null && refs.winLosePanel != null)
            {
                refs.winLosePanel.SetActive(true);
                if (refs.winLoseTitle != null) refs.winLoseTitle.text = "HOÀN THÀNH!";
                if (refs.winLoseSub   != null) refs.winLoseSub.text   = "Đã đưa người dân tới điểm trú.";
            }

            if (txtObjective != null) txtObjective.text = "Hoàn thành! Đã cứu đủ 3 người.";

            ShowFeedback("Đã cứu đủ 3 người. Chúc mừng!");
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Feedback banner
        // ─────────────────────────────────────────────────────────────────────
        public void ShowFeedback(string msg)
        {
            if (refs != null && refs.feedbackText != null)
            {
                refs.feedbackText.text = msg;
                StartCoroutine(ClearFeedback(refs.feedbackText));
            }
        }

        private IEnumerator ClearFeedback(TMP_Text text)
        {
            yield return new WaitForSeconds(2.5f);
            if (text != null && text.text != "") text.text = "";
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Utilities
        // ─────────────────────────────────────────────────────────────────────
        private static TMP_Text FindTMP(string goName)
        {
            var go = GameObject.Find(goName);
            return go != null ? go.GetComponent<TMP_Text>() : null;
        }

        /// Returns the first TMP_Text child of a badge root whose name contains "Text"
        /// (i.e. "RescueCountBadge_Text") — this is the count/label element we want to change.
        private static TMP_Text FindBadgeCountText(GameObject badgeRoot)
        {
            foreach (var t in badgeRoot.GetComponentsInChildren<TMP_Text>())
            {
                if (t.gameObject.name.Contains("Text")) return t;
            }
            // Fallback: return any TMP_Text
            return badgeRoot.GetComponentInChildren<TMP_Text>();
        }
    }
}