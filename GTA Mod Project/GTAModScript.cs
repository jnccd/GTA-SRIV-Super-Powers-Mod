using System;
using GTA;
using GTA.Math;
using System.Windows.Forms;

namespace GTA_Mod_Project
{
    public class GTAModScript : Script
    {
        #region Dont mess with those
        bool Activated;
        bool IsSprinting;
        bool SpaceWasDownLastFrame;
        int SuperJumpChargeTimer;
        int SuperJumpTimer;
        int SuperSprintTimer;
        int PerformanceHeavyOperationCooldown;
        #endregion
        #region Mess with them
        float SuperSprintSpeed = 40f;
        float SuperSprintSpeedAntiHalfLife = 30;
        float SuperSprintRepelRangeSquared = 49;
        float GlidingAccel = 3f;
        float GlidingUplift = 0.08f;
        #endregion
        #region Some Objects and Lists
        Random RDM = new Random();
        Entity[] OrbitArray;
        Entity[] SprintRepelArray;
        #endregion

        public GTAModScript()
        {
            Tick += OnTick;
            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;
            Interval = 0;
            SuperJumpTimer = 100;
        }

        void OnTick(object sender, EventArgs e)
        {
            if (Activated)
            {
                Game.Player.Character.Health = 100;
                if (IsSprinting) { if (!Game.Player.Character.IsInAir) { DoSuperSprint(); SuperSprintTimer++; } else { DoGliding(); } } else { SuperSprintTimer = 0; }
                //if (Game.Player.IsAiming) { Game.TimeScale = 0.25f; } else { Game.TimeScale = 1f; }
                if (Game.Player.Character.Position.Z < 2.75f) { Game.Player.Character.Velocity = 
                        new Vector3(Game.Player.Character.Velocity.X, Game.Player.Character.Velocity.Y, -(Game.Player.Character.Position.Z - 2.75f)); }
                if (Game.Player.Character.HeightAboveGround < -Game.Player.Character.Velocity.Z * 0.25f) { Game.Player.Character.Task.ClearAllImmediately(); }

                // Superjump
                if (!Game.IsKeyPressed(Keys.Space) && SpaceWasDownLastFrame && !Game.Player.Character.IsInAir)
                { Game.Player.Character.ApplyForce(new Vector3(0, 0, 175)); SuperJumpTimer = 1; Game.Player.Character.Weapons.Give(GTA.Native.WeaponHash.Parachute, 5, false, true); }
                if (Game.IsKeyPressed(Keys.Space) && Game.Player.Character.Velocity.Z <= 15 && !Game.Player.Character.IsInAir)
                { SuperJumpChargeTimer += 1 + SuperJumpChargeTimer / 50; }
                if (SuperJumpChargeTimer > 150) { SuperJumpChargeTimer = 150; }
                if (SuperJumpChargeTimer > 25) { UI.ShowSubtitle("Super-Jump: " + SuperJumpChargeTimer.ToString()); }
                if (SuperJumpTimer == 15)
                { Game.Player.Character.Velocity = new Vector3(Game.Player.Character.Velocity.X, Game.Player.Character.Velocity.Y, -0.2f); }
                if (SuperJumpTimer == 18) { Game.Player.Character.ApplyForce(new Vector3(0, 0, SuperJumpChargeTimer + 5)); SuperJumpChargeTimer = 25; }
                if (Game.IsKeyPressed(Keys.Space)) { SpaceWasDownLastFrame = true; } else { SpaceWasDownLastFrame = false; }
                
                PerformanceHeavyOperationCooldown--;
                SuperJumpTimer++;
            }
        }
        void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (Activated)
            {
                if (e.KeyCode == Keys.O)
                {
                    if (PerformanceHeavyOperationCooldown < 0) { OrbitAroundPlayer(); PerformanceHeavyOperationCooldown = 3; }
                    Game.Player.Character.Velocity = Vector3.Zero;
                }
                if (e.KeyCode == Keys.ShiftKey && Game.Player.Character.CurrentVehicle == null && Game.Player.Character.Velocity.LengthSquared() > 0.1f) { IsSprinting = true; }
            }
            if (e.KeyCode == Keys.NumPad1) { SwitchActiavation(); }
        }
        void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ShiftKey) { IsSprinting = false; }
        }

        void OrbitAroundPlayer()
        {
            OrbitArray = World.GetAllEntities();
            for (int i = 0; i < OrbitArray.Length; i++)
            {
                if (OrbitArray[i] != Game.Player.Character)
                {
                    Vector3 PullVector = OrbitArray[i].Position - Game.Player.Character.Position;
                    float ForceMagnitude = 100 / (PullVector.Length() / 2);
                    float ForceAngle = (float)Math.Atan2(PullVector.X, PullVector.Y) + 2.4f;
                    if (ForceMagnitude > 20) { ForceMagnitude = 20; }
                    OrbitArray[i].ApplyForce(new Vector3(ForceMagnitude * (float)Math.Sin(ForceAngle), ForceMagnitude * (float)Math.Cos(ForceAngle), 1.8f));
                }
            }
        }
        void SwitchActiavation()
        {
            if (Activated)
            {
                UI.Notify("Saints Row IV Mods were disabled!");
                Activated = false;
                Game.Player.Character.CanRagdoll = true;
                Game.Player.Character.IsInvincible = false;
            }
            else
            {
                UI.Notify("Saints Row IV Mods were enabled!");
                Activated = true;
                Game.Player.Character.CanRagdoll = false;
                Game.Player.Character.IsInvincible = true;
            }
        }
        void DoSuperSprint()
        {
            float CurrentSprintSpeedPercentage = (-(1 / ((SuperSprintTimer + SuperSprintSpeedAntiHalfLife) / SuperSprintSpeedAntiHalfLife)) + 1);
            float Angle = (float)Math.Atan2(Game.Player.Character.ForwardVector.X, Game.Player.Character.ForwardVector.Y);
            float CurrentSuperSprintSpeed = SuperSprintSpeed * CurrentSprintSpeedPercentage;
            Game.Player.Character.Velocity = new Vector3(Game.Player.Character.Velocity.X / 2.5f + CurrentSuperSprintSpeed * (float)Math.Sin(Angle),
                                                         Game.Player.Character.Velocity.Y / 2.5f + CurrentSuperSprintSpeed * (float)Math.Cos(Angle),
                                                         Game.Player.Character.Velocity.Z - 0.2f);

            SprintRepelArray = World.GetAllVehicles();
            for (int i = 0; i < SprintRepelArray.Length; i++)
            {
                Vector3 PullVector = SprintRepelArray[i].Position - Game.Player.Character.Position;
                float PullVectorLengthSquared = PullVector.LengthSquared();
                if (SprintRepelArray[i] != Game.Player.Character && PullVectorLengthSquared < SuperSprintRepelRangeSquared)
                {
                    float ForceMagnitude = CurrentSprintSpeedPercentage * 300 / PullVectorLengthSquared;
                    float ForceAngle = (float)Math.Atan2(PullVector.X, PullVector.Y);
                    SprintRepelArray[i].ApplyForce(new Vector3(ForceMagnitude * (float)Math.Sin(ForceAngle), ForceMagnitude * (float)Math.Cos(ForceAngle), 2f));
                }
            }
        }
        void DoGliding()
        {
            float Angle = (float)Math.Atan2(Game.Player.Character.ForwardVector.X, Game.Player.Character.ForwardVector.Y);
            Game.Player.Character.ApplyForce(new Vector3(GlidingAccel * (float)Math.Sin(Angle),
                                                         GlidingAccel * (float)Math.Cos(Angle),
                                                         GlidingUplift));
        }
    }
}
