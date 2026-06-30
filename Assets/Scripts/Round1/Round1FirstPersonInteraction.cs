using System.Collections;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
        public float rescueMaxInteractSpeed = 0.35f;

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
        private TMP_Text txtRemaining;
        private TMP_Text txtInteractionPrompt;
        private TMP_Text txtContextToast;
        private CanvasGroup contextToastGroup;
        private Image contextToastAccent;
        private Coroutine feedbackRoutine;

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
        private GameObject nhaBaPeopleRoot;
        private GameObject nhaTuPeopleRoot;

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
            txtRemaining = FindTMP("TXT_R1_Shared_Remaining");
            txtInteractionPrompt = FindTMP("TXT_R1_InteractionPrompt");
            txtContextToast = FindTMP("TXT_R1_ContextToast");

            if (txtInteractionPrompt != null)
                txtInteractionPrompt.transform.parent.gameObject.SetActive(false);

            if (txtContextToast != null)
            {
                contextToastGroup = txtContextToast.GetComponentInParent<CanvasGroup>();
                contextToastAccent = FindImage("IMG_R1_ToastAccent");
                txtContextToast.transform.parent.gameObject.SetActive(false);
            }

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
            nhaBaPeopleRoot = GameObject.Find("R1_RescuePeople_NhaBa_2");
            nhaTuPeopleRoot = GameObject.Find("R1_NhaTu_RescueAnchor");

            // Get the count-label TMP inside each badge (first TMP_Text that shows a number)
            if (nhaBaBadgeRoot != null)
                nhaBaBadgeText = FindBadgeCountText(nhaBaBadgeRoot);
            if (nhaTuBadgeRoot != null)
                nhaTuBadgeText = FindBadgeCountText(nhaTuBadgeRoot);

            // Set bottom instruction bar
            SetMsg("<b>WASD</b>  Lái thuyền     <b>E</b>  Cứu / Thả     <b>TAB</b>  Bản đồ     <b>Q</b>  Chờ");

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
            float maxInteractSpeed = rescueMaxInteractSpeed;

            int cargo = realtimeController != null ? realtimeController.currentCargo : 0;
            int capacity = realtimeController != null ? realtimeController.boatCapacity : 3;
            string objText = realtimeController != null ? realtimeController.currentObjectiveText : "";

            if (insideRescueZoneA)
            {
                if (realtimeController != null && !realtimeController.rescuedA && objText.Contains("Nhà cần cứu 1"))
                {
                    if (boatSpeed <= maxInteractSpeed)
                    {
                        prompt = "Nhấn E để cứu 2 người.";
                        if (ePressed)
                        {
                            if (realtimeController.TryRescueA(2))
                            {
                                ShowFeedback("Đã cứu 2 người lên thuyền.");
                                if (nhaBaBadgeRoot != null) nhaBaBadgeRoot.SetActive(false);
                                if (nhaBaPeopleRoot != null) nhaBaPeopleRoot.SetActive(false);
                                prompt = ""; // cleared
                            }
                        }
                    }
                    else
                    {
                        prompt = "Dừng thuyền để cứu người an toàn.";
                    }
                }
            }
            else if (insideRescueZoneB)
            {
                if (realtimeController != null && !realtimeController.rescuedB && objText.Contains("Nhà cần cứu 2"))
                {
                    if (boatSpeed <= maxInteractSpeed)
                    {
                        prompt = "Nhấn E để cứu 1 người.";
                        if (ePressed)
                        {
                            if (realtimeController.TryRescueB(1))
                            {
                                ShowFeedback("Đã cứu thêm 1 người.");
                                if (nhaTuBadgeRoot != null) nhaTuBadgeRoot.SetActive(false);
                                if (nhaTuPeopleRoot != null) nhaTuPeopleRoot.SetActive(false);
                                prompt = ""; // cleared
                            }
                        }
                    }
                    else
                    {
                        prompt = "Dừng thuyền để cứu người an toàn.";
                    }
                }
            }
            else if (insideShelterZone)
            {
                if (objText.ToLower().Contains("điểm trú"))
                {
                    if (cargo <= 0)
                    {
                        prompt = "Chưa có người dân trên thuyền.";
                    }
                    else
                    {
                        if (boatSpeed <= maxInteractSpeed)
                        {
                            prompt = "Nhấn E để đưa người dân vào điểm trú.";
                            if (ePressed)
                            {
                                if (realtimeController.TryDropOff())
                                {
                                    ShowFeedback("Đã đưa 3 người dân vào điểm trú an toàn.");
                                    prompt = "";
                                }
                            }
                        }
                        else
                        {
                            prompt = "Dừng thuyền để cập bến an toàn.";
                        }
                    }
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
            SetObjective();
            SetInteractionPrompt(prompt);
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

        private bool insideRescueZoneA = false;
        private bool insideRescueZoneB = false;
        private bool insideShelterZone = false;

        private void OnTriggerEnter(Collider other)
        {
            if (other.name == "R1_RescueZone_A") insideRescueZoneA = true;
            else if (other.name == "R1_RescueZone_B") insideRescueZoneB = true;
            else if (other.name == "R1_ShelterDropoffZone") insideShelterZone = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.name == "R1_RescueZone_A") insideRescueZoneA = false;
            else if (other.name == "R1_RescueZone_B") insideRescueZoneB = false;
            else if (other.name == "R1_ShelterDropoffZone") insideShelterZone = false;
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
                    txtTurn.text = string.Format("{0:00}:{1:00}", mins, secs);
                }

                if (txtDurability != null)
                {
                    txtDurability.text = $"{realtimeController.currentBoatDurability}/{realtimeController.maxBoatDurability}";
                }

                if (txtCargo != null)
                    txtCargo.text = $"{realtimeController.currentCargo}/{realtimeController.boatCapacity}";
                
                if (txtSaved != null)
                    txtSaved.text = $"{realtimeController.civiliansSafe}/{realtimeController.totalCivilians}";

                if (txtRemaining != null)
                    txtRemaining.text = Mathf.Max(0, realtimeController.totalCivilians - realtimeController.civiliansSafe).ToString();
            }
        }

        public void HideGameplayHUD()
        {
            if (txtTurn != null && txtTurn.transform.parent != null) txtTurn.transform.parent.gameObject.SetActive(false);
            if (txtMessage != null && txtMessage.transform.parent != null) txtMessage.transform.parent.gameObject.SetActive(false);
            if (txtInteractionPrompt != null && txtInteractionPrompt.transform.parent != null) txtInteractionPrompt.transform.parent.gameObject.SetActive(false);
            if (txtContextToast != null && txtContextToast.transform.parent != null) txtContextToast.transform.parent.gameObject.SetActive(false);
        }

        private void SetObjective()
        {
            if (txtObjective == null) return;

            if (realtimeController != null)
                txtObjective.text = realtimeController.currentObjectiveText;
        }

        private void SetInteractionPrompt(string prompt)
        {
            if (txtInteractionPrompt == null) return;

            bool visible = !string.IsNullOrEmpty(prompt);
            var root = txtInteractionPrompt.transform.parent.gameObject;
            if (root.activeSelf != visible) root.SetActive(visible);
            if (visible) txtInteractionPrompt.text = prompt;
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
            if (txtContextToast != null && contextToastGroup != null)
            {
                if (feedbackRoutine != null) StopCoroutine(feedbackRoutine);
                feedbackRoutine = StartCoroutine(ShowToast(msg, 3f));
                return;
            }

            if (refs != null && refs.feedbackText != null)
            {
                refs.feedbackText.text = msg;
                StartCoroutine(ClearFeedback(refs.feedbackText));
            }
        }

        private IEnumerator ShowToast(string message, float holdDuration)
        {
            var root = txtContextToast.transform.parent.gameObject;
            root.SetActive(true);
            txtContextToast.text = message;

            bool critical = message.Contains("hỏng") || message.Contains("Va chạm") ||
                            message.Contains("Dừng") || message.Contains("đầy");
            if (contextToastAccent != null)
                contextToastAccent.color = critical
                    ? new Color(0.52f, 0.28f, 0.20f, 1f)
                    : new Color(0.67f, 0.52f, 0.24f, 1f);

            contextToastGroup.alpha = 0f;
            const float fadeIn = 0.16f;
            const float fadeOut = 0.28f;
            float elapsed = 0f;
            while (elapsed < fadeIn)
            {
                elapsed += Time.unscaledDeltaTime;
                contextToastGroup.alpha = Mathf.Clamp01(elapsed / fadeIn);
                yield return null;
            }

            contextToastGroup.alpha = 1f;
            yield return new WaitForSecondsRealtime(holdDuration);

            elapsed = 0f;
            while (elapsed < fadeOut)
            {
                elapsed += Time.unscaledDeltaTime;
                contextToastGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOut);
                yield return null;
            }

            contextToastGroup.alpha = 0f;
            root.SetActive(false);
            feedbackRoutine = null;
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
            foreach (var text in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (text.name == goName && text.gameObject.scene.isLoaded) return text;
            return null;
        }

        private static Image FindImage(string goName)
        {
            foreach (var image in FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (image.name == goName && image.gameObject.scene.isLoaded) return image;
            return null;
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
