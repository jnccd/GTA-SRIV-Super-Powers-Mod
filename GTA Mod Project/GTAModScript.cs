using System;
using GTA;
using GTA.Math;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections.Generic;

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
        int SprintRepelCheckCube = 12;
        bool WasQPressedLastFrame;
        float TelekinesisModelSize;
        long DebugTime;
        int Timer;
        #endregion
        #region Mess with them
        float SuperSprintSpeed = 48f;
        float SuperSprintInvertedAcc = 100;
        float SuperSprintRepelRangeSquared = 49;
        float GlidingAccel = 2.5f;
        float GlidingUplift = 0.095f;
        #endregion
        #region Some Objects and Lists
        Random RDM = new Random();
        Entity[] OrbitArray;
        Entity[] SprintRepelArray;
        Entity TelekinesisEntity = null;
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
            try
            {
                if (Activated)
                {
                    Game.Player.Character.Health = 100;

                    if (IsSprinting)
                    {
                        if (!Game.Player.Character.IsInAir)
                            DoSuperSprint();
                        else
                            DoGliding();

                        // Rendering Camera is fake news
                        //Game.Player.Character.Rotation = new Vector3(Game.Player.Character.Rotation.X, Game.Player.Character.Rotation.Y, World.RenderingCamera.Rotation.Z);

                        SuperSprintTimer++;
                    }
                    else
                        SuperSprintTimer = 0;

                    //if (Game.Player.IsAiming) { Game.TimeScale = 0.25f; } else { Game.TimeScale = 1f; }
                    if (Game.Player.Character.Position.Z < 2.75f)
                    {
                        Game.Player.Character.Velocity = new Vector3(Game.Player.Character.Velocity.X, Game.Player.Character.Velocity.Y,
                            -(Game.Player.Character.Position.Z - 2.75f));
                        if (Game.Player.Character.IsSwimming || Game.Player.Character.IsInParachuteFreeFall)
                            Game.Player.Character.Task.ClearAllImmediately();
                    }

                    // No fall dmg anim
                    if (Game.Player.Character.IsInParachuteFreeFall && Game.Player.Character.Velocity.Z < 0 &&
                        Game.Player.Character.HeightAboveGround < 2 ||

                        Game.Player.Character.IsFalling && Game.Player.Character.Velocity.Z < 0 &&
                        Game.Player.Character.HeightAboveGround < 2 ||

                        Game.Player.Character.IsGettingUp)
                    {
                        ResetPlayerAnim();
                    }

                    // Superjump
                    if (!Game.IsKeyPressed(Keys.Space) && SpaceWasDownLastFrame && !Game.Player.Character.IsInAir)
                    {
                        float Percentage = -(1 / ((SuperSprintTimer + SuperSprintInvertedAcc) / SuperSprintInvertedAcc)) + 1;
                        Game.Player.Character.Weapons.Give(GTA.Native.WeaponHash.Parachute, 1, false, true);
                        Game.Player.Character.Task.Skydive();
                        Game.Player.Character.ApplyForce(new Vector3(0, 0, SuperJumpChargeTimer * Percentage + 5) + Game.Player.Character.ForwardVector * (SuperJumpChargeTimer * Percentage / 2.5f));
                        SuperJumpTimer = 0;
                        SuperJumpChargeTimer = 25;
                    }

                    if (SuperJumpChargeTimer == 26)
                        ResetPlayerAnim();

                    if (Game.IsKeyPressed(Keys.Space)/* && Game.Player.Character.Velocity.Z <= 15*/ && !Game.Player.Character.IsInAir)
                        SuperJumpChargeTimer += 1 + SuperJumpChargeTimer / 50;

                    if (SuperJumpChargeTimer > 450)
                        SuperJumpChargeTimer = 450;

                    if (SuperJumpChargeTimer > 25)
                        UI.ShowSubtitle("Super-Jump: " + SuperJumpChargeTimer.ToString());
                    else
                    {
                        string output = "";
                        try
                        {
                            output += "CanRagdoll: " + Game.Player.Character.CanRagdoll.ToString();
                            if (TelekinesisEntity == null)
                                output += " | TelekinesisEntity: null";
                            else
                                output += " | TelekinesisEntity: " + TelekinesisEntity.Model.ToString();
                            output += " | PlayerIsSetToGround: " + (!Game.Player.Character.IsInAir && SuperJumpTimer > 100 && Game.Player.Character.Position.Z > 3 || Game.Player.Character.IsFalling).ToString();
                            output += " | IsFalling: " + Game.Player.Character.IsFalling.ToString();
                            output += " | HeightAboveGround: " + Math.Round(Game.Player.Character.HeightAboveGround, 1).ToString();
                            output += " | TelekinesisModelSize: " + Math.Round(TelekinesisModelSize, 1).ToString();
                            output += " | (FakeNews)RenderingCameraPos: " + World.RenderingCamera.Position.ToString();
                        }
                        catch (Exception ex) { output += ex.Message + ex.InnerException + ex.StackTrace; }

                        UI.ShowSubtitle(output);
                    }

                    // Telekinesis
                    if (Game.IsKeyPressed(Keys.Q) && !WasQPressedLastFrame)
                    {
                        ResetPlayerAnim();
                        if (TelekinesisEntity == null)
                        {
                            RaycastResult RayResult = World.GetCrosshairCoordinates();
                            if (RayResult.DitHitEntity && RayResult.HitEntity != Game.Player.Character)
                            {
                                TelekinesisEntity = RayResult.HitEntity;
                                TelekinesisEntity.HasGravity = true;
                                if (TelekinesisEntity.GetType() == typeof(Ped))
                                    ((Ped)TelekinesisEntity).Task.Jump();
                                Vector3 Dimensions = TelekinesisEntity.Model.GetDimensions();

                                TelekinesisModelSize = (float)Math.Sqrt(Dimensions.X * Dimensions.X + Dimensions.Y * Dimensions.Y);
                            }
                        }
                        else
                        {
                            TelekinesisEntity.ApplyForce((TelekinesisEntity.Position - Game.Player.Character.Position) * 20 + Game.Player.Character.Velocity);
                            TelekinesisEntity = null;
                        }
                    }
                    if (TelekinesisEntity != null)
                    {
                        RaycastResult RayResult = World.GetCrosshairCoordinates();
                        if (RayResult.DitHitAnything && !RayResult.DitHitEntity || RayResult.DitHitEntity && RayResult.HitEntity != Game.Player.Character)
                        {
                            Vector3 Hit = RayResult.HitCoords;
                            Vector3 ToHit = Hit - Game.Player.Character.Position;
                            ToHit.Normalize();
                            TelekinesisEntity.Position = Game.Player.Character.Position + ToHit * (TelekinesisModelSize + 0.5f);
                        }
                        else
                            TelekinesisEntity.Position = Game.Player.Character.Position + Game.Player.Character.ForwardVector * (TelekinesisModelSize + 0.5f);
                    }

                    // Setting to Ground
                    if (!Game.Player.Character.IsInAir && SuperJumpTimer > 100 && Game.Player.Character.Position.Z > 3 || Game.Player.Character.IsFalling)
                    {
                        Game.Player.Character.Velocity += new Vector3(0, 0, (-Game.Player.Character.HeightAboveGround + 1)*2);
                    }

                    // Safety Reset
                    if (Game.IsKeyPressed(Keys.D0))
                    {
                        Game.Player.Character.Position = new Vector3(0, 0, 100);
                        Game.Player.Character.Velocity = Vector3.Zero;
                        TelekinesisEntity = null;
                    }

                    // Last Instructions
                    if (Game.IsKeyPressed(Keys.Space))
                        SpaceWasDownLastFrame = true;
                    else
                        SpaceWasDownLastFrame = false;

                    if (Game.IsKeyPressed(Keys.Q))
                        WasQPressedLastFrame = true;
                    else
                        WasQPressedLastFrame = false;

                    PerformanceHeavyOperationCooldown--;
                    SuperJumpTimer++;
                    Timer++;
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message + "\n\n\n" + ex.InnerException + "\n\n\n" + ex.StackTrace);
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
                if (e.KeyCode == Keys.ShiftKey && Game.Player.Character.CurrentVehicle == null && Game.Player.Character.Velocity.LengthSquared() > 0.07f) { IsSprinting = true; }
            }
            if (e.KeyCode == Keys.NumPad1) { SwitchActiavation(); }
        }
        void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ShiftKey) { IsSprinting = false; }
        }
        
        void ResetPlayerAnim()
        {
            if (Game.Player.Character.IsInParachuteFreeFall)
            {
                Game.Player.Character.Task.Skydive();
            }
            else
            {
                Game.Player.Character.Task.ClearAllImmediately();
            }
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
                    float ForceAngle = (float)Math.Atan2(PullVector.X, PullVector.Y) + 2.7f;
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
            float CurrentSprintSpeedPercentage = -(1 / ((SuperSprintTimer + SuperSprintInvertedAcc) / SuperSprintInvertedAcc)) + 1;
            float Angle = (float)Math.Atan2(Game.Player.Character.ForwardVector.X, Game.Player.Character.ForwardVector.Y);
            float CurrentSuperSprintSpeed = SuperSprintSpeed * CurrentSprintSpeedPercentage;
            Game.Player.Character.Velocity = new Vector3(Game.Player.Character.Velocity.X / 3f + CurrentSuperSprintSpeed * (float)Math.Sin(Angle),
                                                         Game.Player.Character.Velocity.Y / 3f + CurrentSuperSprintSpeed * (float)Math.Cos(Angle),
                                                         //Game.Player.Character.Velocity.Z - 0.2f);
                                                         Game.Player.Character.Velocity.Z);

            if (Timer % 5 == 0)
            {
                switch (RDM.Next(0, 4))
                {
                    case 0:
                        World.AddExplosion(Game.Player.Character.Position + Game.Player.Character.Velocity / 5, ExplosionType.Extinguisher, 5f, 0, false, false);
                        break;

                    case 1:
                        World.AddExplosion(Game.Player.Character.Position + Game.Player.Character.Velocity / 5, ExplosionType.Molotov1, 5f, 0, false, false);
                        break;

                    case 2:
                        World.AddExplosion(Game.Player.Character.Position + Game.Player.Character.Velocity / 5, ExplosionType.SmokeG, 5f, 0, false, false);
                        break;

                    case 3:
                        World.AddExplosion(Game.Player.Character.Position + Game.Player.Character.Velocity / 5, ExplosionType.SnowBall, 5f, 0, false, false);
                        break;

                    case 4:
                        World.AddExplosion(Game.Player.Character.Position + Game.Player.Character.Velocity / 5, ExplosionType.Steam, 5f, 0, false, false);
                        break;
                }
            }
            
            SprintRepelArray = World.GetAllVehicles();
            for (int i = 0; i < SprintRepelArray.Length; i++)
            {
                if (SprintRepelArray[i] != Game.Player.Character &&
                    
                    SprintRepelArray[i].Position.X + SprintRepelCheckCube > Game.Player.Character.Position.X &&
                    SprintRepelArray[i].Position.X - SprintRepelCheckCube < Game.Player.Character.Position.X &&

                    SprintRepelArray[i].Position.Y + SprintRepelCheckCube > Game.Player.Character.Position.Y &&
                    SprintRepelArray[i].Position.Y - SprintRepelCheckCube < Game.Player.Character.Position.Y &&

                    SprintRepelArray[i].Position.Z + SprintRepelCheckCube > Game.Player.Character.Position.Z &&
                    SprintRepelArray[i].Position.Z - SprintRepelCheckCube < Game.Player.Character.Position.Z)
                {
                    Vector3 PullVector = SprintRepelArray[i].Position - Game.Player.Character.Position;
                    float PullVectorLengthSquared = PullVector.LengthSquared();
                    if (PullVectorLengthSquared < SuperSprintRepelRangeSquared)
                    {
                        float ForceMagnitude = CurrentSprintSpeedPercentage * 300 / PullVectorLengthSquared;
                        PullVector.Normalize();
                        SprintRepelArray[i].ApplyForce(PullVector * ForceMagnitude + Game.Player.Character.Velocity / 24f);
                    }
                }
            }
        }
        void DoGliding()
        {
            float CurrentGlidingAccPercentage = -(1 / ((SuperSprintTimer + SuperSprintInvertedAcc) / SuperSprintInvertedAcc)) + 1;

            float Angle = (float)Math.Atan2(Game.Player.Character.ForwardVector.X, Game.Player.Character.ForwardVector.Y);
            Game.Player.Character.ApplyForce(new Vector3(GlidingAccel * CurrentGlidingAccPercentage * (float)Math.Sin(Angle),
                                                         GlidingAccel * CurrentGlidingAccPercentage * (float)Math.Cos(Angle),
                                                         GlidingUplift));
        }
    }
}
