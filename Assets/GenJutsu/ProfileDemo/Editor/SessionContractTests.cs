using NUnit.Framework;
using GenJutsu.ProfileDemo;

namespace GenJutsu.ProfileDemo.EditorTools
{
    public class SessionContractTests
    {
        const string Active = "{\"session\":{\"id\":\"AB3K7Q\",\"session_code\":\"AB3K7Q\",\"display_name\":\"Lina\",\"personality_text\":\"Lina, o dia abre em água parada.\",\"archetype\":\"Zen\",\"profile_id\":\"Zen\",\"status\":\"active\"}}";

        [Test]
        public void WrappedActiveSession_IsReady_AndKeyIsTheVisitId()
        {
            var result = SessionApiClient.Parse(Active, 200, true);

            Assert.AreEqual(SessionApiClient.Kind.Ready, result.kind);
            Assert.AreEqual(ProfileId.Zen, result.profile);
            Assert.AreEqual("Lina", result.displayName);
            Assert.AreEqual("AB3K7Q", result.sessionCode);
            Assert.AreEqual("AB3K7Q", result.sessionKey);
        }

        [Test]
        public void SameProfile_DifferentVisitId_IsADifferentSession()
        {
            var again = "{\"session\":{\"id\":\"ZZ99AA\",\"session_code\":\"ZZ99AA\",\"display_name\":\"Lina\",\"profile_id\":\"Zen\",\"status\":\"active\"}}";

            var first = SessionApiClient.Parse(Active, 200, true);
            var second = SessionApiClient.Parse(again, 200, true);

            Assert.AreEqual(ProfileId.Zen, first.profile);
            Assert.AreEqual(ProfileId.Zen, second.profile);
            Assert.AreNotEqual(first.sessionKey, second.sessionKey);
        }

        [Test]
        public void SameVisitId_IsNotANewSession_EvenIfTheCodeFieldDiffers()
        {
            var first = "{\"session\":{\"id\":\"v1\",\"session_code\":\"EXU4RN\",\"profile_id\":\"Zen\",\"status\":\"active\"}}";
            var codeOnly = "{\"session\":{\"id\":\"v1\",\"session_code\":\"ZZ99AA\",\"profile_id\":\"Acolhedor\",\"status\":\"active\"}}";
            var idChanged = "{\"session\":{\"id\":\"v2\",\"session_code\":\"EXU4RN\",\"profile_id\":\"Zen\",\"status\":\"active\"}}";

            Assert.AreEqual(SessionApiClient.SessionKeyFrom(first), SessionApiClient.SessionKeyFrom(codeOnly));
            Assert.AreNotEqual(SessionApiClient.SessionKeyFrom(first), SessionApiClient.SessionKeyFrom(idChanged));
        }

        [Test]
        public void NullSession_IsNoActiveVisit_EvenOnHttp200()
        {
            var result = SessionApiClient.Parse("{\"session\":null}", 200, true);

            Assert.AreEqual(SessionApiClient.Kind.WaitingForAdmin, result.kind);
            Assert.IsFalse(result.hasProfile);
            Assert.AreEqual("", SessionApiClient.SessionKeyFrom("{\"session\":null}"));
        }

        [Test]
        public void Legacy404_IsStillNoActiveVisit()
        {
            var result = SessionApiClient.Parse("{\"error\":\"no_current_session\"}", 404, false);

            Assert.AreEqual(SessionApiClient.Kind.WaitingForAdmin, result.kind);
        }

        [Test]
        public void EndedStatus_ClosesEvenIfProfileIsPresent()
        {
            var body = "{\"session\":{\"id\":\"AB3K7Q\",\"session_code\":\"AB3K7Q\",\"display_name\":\"Lina\",\"profile_id\":\"Zen\",\"status\":\"ended\"}}";
            var result = SessionApiClient.Parse(body, 200, true);
            Assert.AreEqual(SessionApiClient.Kind.Ended, result.kind);
            Assert.AreEqual("AB3K7Q", result.sessionKey);
        }

        [Test]
        public void ActiveStatus_IsNotClosed()
        {
            Assert.IsFalse(SessionApiClient.IsClosedSession(
                "{\"id\":\"AB3K7Q\",\"profile_id\":\"Zen\",\"status\":\"active\"}"));
        }

        [Test]
        public void LegacyFlatSummary_StillResolves()
        {
            var result = SessionApiClient.Parse(
                "{\"session_code\":\"EXU4RN\",\"display_name\":\"QA Prints\",\"profile_id\":\"Acolhedor\"}",
                200, true);

            Assert.AreEqual(SessionApiClient.Kind.Ready, result.kind);
            Assert.AreEqual(ProfileId.Acolhedor, result.profile);
            Assert.AreEqual("EXU4RN", result.sessionKey);
        }

        [Test]
        public void TransportFailure_DoesNotLookLikeAnEndedSession()
        {
            var result = SessionApiClient.Parse("", 0, false);
            Assert.AreEqual(SessionApiClient.Kind.Down, result.kind);
        }

        [Test]
        public void QuestClarity_KeepsMsaaAndEyeScale()
        {
            Assert.AreEqual(4, QuestClarity.Msaa);
            Assert.AreEqual(1.15f, QuestClarity.EyeScale, 0.001f);
        }

        [Test]
        public void WelcomeTitle_UsesTheName_AndNeverTheWaitingLine()
        {
            Assert.AreEqual("Bem-vindo", SessionGate.TitleFor(null));
            Assert.AreEqual("Bem-vindo", SessionGate.TitleFor("  "));
            Assert.AreEqual("Bem-vindo, Lina", SessionGate.TitleFor(" Lina "));
            StringAssert.DoesNotContain("anfitri", SessionGate.TitleFor("Lina").ToLowerInvariant());
        }

        [Test]
        public void Grabbables_RestKinematicUntilGrabbed()
        {
            var go = UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cube);
            var grab = GrabUtil.Make(go, 0.2f);
            var body = go.GetComponent<UnityEngine.Rigidbody>();
            Assert.IsNotNull(grab);
            Assert.IsTrue(body.isKinematic);
            Assert.IsFalse(body.useGravity);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void WelcomeCard_StaysInvisibleWhenEnsured()
        {
            var go = new UnityEngine.GameObject("SessionGateTest", typeof(UnityEngine.RectTransform));
            var canvas = go.AddComponent<UnityEngine.Canvas>();
            canvas.renderMode = UnityEngine.RenderMode.WorldSpace;
            var group = go.AddComponent<UnityEngine.CanvasGroup>();
            group.alpha = 1f;
            var gate = go.AddComponent<SessionGate>();
            gate.group = group;
            gate.suppressNetworkPoll = true;
            gate.EnsureVisible();
            Assert.AreEqual(0f, group.alpha, 0.001f);
            Assert.IsFalse(canvas.enabled);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void ProfileId_IsNotTheVisitId()
        {
            var key = SessionApiClient.SessionKeyFrom(
                "{\"session\":{\"id\":\"VISIT1\",\"profile_id\":\"Zen\",\"session_code\":\"EXU4RN\",\"status\":\"active\"}}");
            Assert.AreEqual("VISIT1", key);
        }
    }
}
