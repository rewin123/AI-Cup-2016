using Com.CodeGame.CodeWizards2016.DevKit.CSharpCgdk.Model;
using System;
//using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Net.Sockets;
using System.Text;
//using System.Drawing;

namespace Com.CodeGame.CodeWizards2016.DevKit.CSharpCgdk {
    
    public sealed class MyStrategy : IStrategy {
        //�������� ������ �������
        //��������� ������������������ ������� �� ���������� � ��� ������
        Vector ithinkpos = Vector.Zero;
        bool debug = true;
        string debugpath = "debug.txt";
        //StreamWriter wr;
        Random r = new Random();
        //���������� ������ ���������

        //������� � ���������� ������
        //next position = position + speed
        //next angle = angle + turn
        Cell bestcell = null;
        //Drawer draw;
        LaneType myline;
        Vector[] attackpath = null;
        Cell c = null;
        AbstractMove absmove = null;
        Vector[] path;
        int pospath = 0;
        AbstractMoveType type = AbstractMoveType.GoTo;
        Faction enemyFaction;
        Vector prevpos = new Vector();

        delegate bool LineFilter(Vector pos, ref World world);
        LineFilter filter;

        public MyStrategy()
        {
            //if (debug)
            //{
            //    draw = new Drawer();

            //    draw.Show();
            //}


        }
        public void Move(Wizard self, World world, Game game, Move move) {
            myline = LaneType.Top;
            if (world.TickIndex < 400)
            {
                return;
            }
            else if (world.TickIndex == 400)
            {
                if (self.Faction == Faction.Academy)
                    enemyFaction = Faction.Renegades;
                else enemyFaction = Faction.Academy;
                SelectLine(world, self, game);
            }
            Cell.game = game;
            if(self.Life < self.MaxLife * 0.25)
            {
                absmove.type = AbstractMoveType.GoTo;
                path = RunOut(new Vector(self.X, self.Y), enemyFaction, new MyWorld(world,self), 9, new MyCircularUnit(self), (int)game.WizardCastRange);
            }
            //if (attackpath == null)
            //{
            //    if (self.Faction == Faction.Academy)
            //        enemyFaction = Faction.Renegades;
            //    else enemyFaction = Faction.Academy;

            //    attackpath = new Vector[6];
            //    attackpath[0] = new Vector(100, world.Height - 100);
            //    attackpath[1] = new Vector(100, world.Height - 800);
            //    attackpath[2] = new Vector(100, world.Height * 0.5);
            //    attackpath[3] = new Vector(200, 400);
            //    attackpath[4] = new Vector(world.Width * 0.5, 200);
            //    attackpath[5] = new Vector(world.Width - 200, 200);

            //    absmove = new AbstractMove();
            //    absmove.type = AbstractMoveType.GoTo;
            //    absmove.target = attackpath[3];

            //    MyCircularUnit unit = new MyCircularUnit(self);
            //    //unit.radius += 2;
            //    path = FindPath(new Vector(self.X, self.Y), attackpath[3], new MyWorld(world, self), 12, unit);
            //    absmove.target = attackpath[3];
            //    absmove.type = AbstractMoveType.GoTo;
            //    //DrawVectorPath(path, (int)350, new Vector(self.X - 150,self.Y - 150), world,unit);
            //    filter = TopFilter;
            //}
            if (absmove.changeWithEnemiesInCastRange)
            {
                double dist = MinRangeFromFaction(new Vector(self.X, self.Y), new MyWorld(world,self), enemyFaction);
                if(dist <= game.WizardCastRange)
                {
                    absmove.type = absmove.nextType;
                    absmove.changeWithEnemiesInCastRange = false;
                }
            }
            if (absmove.type == AbstractMoveType.GoTo)
            {
                
                MyCircularUnit unit = new MyCircularUnit(self);
                //unit.radius += 6;
                MyWorld myworld = new MyWorld(world, self);
                if (myworld.TestCollide(unit))
                {
                    unit.pos = absmove.target;
                    if (!myworld.TestCollide(unit))
                    {
                        path = FindPath(new Vector(self.X, self.Y), absmove.target, new MyWorld(world, self), 9, unit);
                        pospath = 0;
                    }
                    else
                    {
                        absmove.type = AbstractMoveType.StendUp;
                    }
                }
                else if (NearestI(world, self, game))
                {
                    absmove.type = AbstractMoveType.GoTo;
                    unit = new MyCircularUnit(self);
                    //unit.radius += 2;
                    path = RunOut(new Vector(self.X, self.Y), enemyFaction, new MyWorld(world, self), 9, unit, (int)game.WizardCastRange);
                    if (path.Length <= 1)
                    {
                        absmove.type = AbstractMoveType.StendUp;
                    }
                    else
                    {
                        pospath = 0;
                        absmove.target = path[path.Length - 1];
                    }
                }
                else if (prevpos.x == self.X && prevpos.y == self.Y)
                {
                    path = FindPath(new Vector(self.X, self.Y), absmove.target, new MyWorld(world, self), 9, new MyCircularUnit(self));
                    pospath = 0;
                }
                //unit.radius -= 6;
                //DrawVectorPath(path, 350, new Vector(self.X - 150, self.Y - 150), world, unit);
                if (FollowPath(path, ref pospath, self, world, game, move))
                {
                    absmove.type = AbstractMoveType.StendUp;
                }
            }
            if (absmove.type == AbstractMoveType.StendUp)
            {

                LivingUnit nearestenemy = null;
                double mindist = double.MaxValue;
                for (int i = 1; i < world.Minions.Length; i++)
                {
                    if (world.Minions[i].Faction == enemyFaction)
                    {
                        if (nearestenemy != null)
                        {
                            double dist = Vector.Distance(self, world.Minions[i]);
                            if (mindist > dist)
                            {
                                mindist = dist;
                                nearestenemy = world.Minions[i];
                            }
                        }
                        else
                        {
                            nearestenemy = world.Minions[i];
                            mindist = Vector.Distance(nearestenemy, self);
                        }
                    }
                }

                for (int i = 0; i < world.Wizards.Length; i++)
                {
                    if (world.Wizards[i].Faction == enemyFaction)
                    {
                        if (nearestenemy != null)
                        {
                            double dist = Vector.Distance(self, world.Wizards[i]);
                            if (mindist > dist)
                            {
                                mindist = dist;
                                nearestenemy = world.Wizards[i];
                            }
                        }
                        else
                        {
                            nearestenemy = world.Wizards[i];
                            mindist = Vector.Distance(nearestenemy, self);
                        }
                    }
                }

                for (int i = 0; i < world.Buildings.Length; i++)
                {
                    if (world.Buildings[i].Faction == enemyFaction)
                    {
                        if (nearestenemy != null)
                        {

                            double dist = Vector.Distance(self, world.Buildings[i]);
                            if (mindist > dist)
                            {
                                mindist = dist;
                                nearestenemy = world.Buildings[i];
                            }
                        }
                        else
                        {
                            nearestenemy = world.Buildings[i];
                            mindist = Vector.Distance(nearestenemy, self);
                        }
                    }
                }

                if (nearestenemy != null)
                {
                    mindist = Vector.Distance(self, nearestenemy);
                    bool nearIsI = true;
                    for (int i = 0; i < world.Minions.Length; i++)
                    {
                        if (world.Minions[i].Faction == self.Faction)
                        {
                            double dist = Vector.Distance(world.Minions[i], nearestenemy);
                            if (dist < mindist)
                            {
                                nearIsI = false;
                                break;
                            }
                        }
                    }
                    if (nearIsI)
                    {
                        for (int i = 0; i < world.Buildings.Length; i++)
                        {
                            if (world.Buildings[i].Faction == self.Faction)
                            {
                                double dist = Vector.Distance(world.Buildings[i], nearestenemy);
                                if (dist < mindist)
                                {
                                    nearIsI = false;
                                    break;
                                }
                            }
                        }
                    }

                    if (nearIsI)
                    {
                        absmove.type = AbstractMoveType.GoTo;
                        MyCircularUnit unit = new MyCircularUnit(self);
                        //unit.radius += 2;
                        path = RunOut(new Vector(self.X, self.Y), enemyFaction, new MyWorld(world, self), 9, unit, (int)game.MinionVisionRange);
                        pospath = 0;
                        absmove.target = path[path.Length - 1];
                    }
                    else
                    {
                        bool wizardInRange = false;
                        wizardInRange = nearestenemy is Wizard;

                        if (wizardInRange == false)
                        {
                            for (int i = 0; i < world.Minions.Length; i++)
                            {
                                if (world.Minions[i].Faction == enemyFaction)
                                {
                                    double dist = Vector.Distance(self, world.Minions[i]);
                                    if (dist <= game.WizardCastRange)
                                    {
                                        if (nearestenemy.Life > world.Minions[i].Life)
                                        {
                                            nearestenemy = world.Minions[i];
                                        }
                                    }
                                }
                            }
                        }
                        for (int i = 0; i < world.Wizards.Length; i++)
                        {
                            if (world.Wizards[i].Faction == enemyFaction)
                            {
                                double dist = Vector.Distance(self, world.Wizards[i]);
                                if (dist <= game.WizardCastRange)
                                {
                                    if (wizardInRange)
                                    {
                                        if(nearestenemy.Life > world.Wizards[i].Life)
                                            nearestenemy = world.Wizards[i];
                                    }
                                    else
                                    {
                                        wizardInRange = true;
                                        nearestenemy = world.Wizards[i];
                                    }
                                }

                            }
                        }
                        if (wizardInRange == false)
                        {
                            for (int i = 0; i < world.Buildings.Length; i++)
                            {
                                if (world.Buildings[i].Faction == enemyFaction)
                                {
                                    double dist = Vector.Distance(self, world.Buildings[i]);
                                    if (dist <= game.WizardCastRange)
                                    {
                                        if(nearestenemy.Life > world.Buildings[i].Life)
                                            nearestenemy = world.Buildings[i];
                                    }
                                }
                            }
                        }
                        if (Vector.Distance(self, nearestenemy) <= game.WizardCastRange)
                        {
                            double angle = self.GetAngleTo(nearestenemy);
                            move.Turn = angle;
                            if (Math.Abs(angle) <= game.StaffSector / 2)
                            {
                                move.Action = ActionType.MagicMissile;
                                move.CastAngle = angle;
                                move.MinCastDistance = Vector.Distance(self, nearestenemy) - nearestenemy.Radius + game.MagicMissileRadius;
                            }
                        }
                        else
                        {
                            if (filter(new Vector(nearestenemy.X,nearestenemy.Y), ref world))
                            {
                                absmove.type = AbstractMoveType.GoTo;
                                MyCircularUnit unit = new MyCircularUnit(self);
                                unit.radius += 2;
                                path = RunIn(new Vector(self.X, self.Y), enemyFaction, new MyWorld(world, self), 9, unit, (int)game.WizardCastRange);
                                pospath = 0;
                                absmove.target = path[path.Length - 1];
                                absmove.changeWithEnemiesInCastRange = true;
                                absmove.nextType = AbstractMoveType.StendUp;
                            }
                            else
                            {
                                GoToAttakPath(world, self, game);
                                absmove.changeWithEnemiesInCastRange = true;
                                absmove.nextType = AbstractMoveType.StendUp;
                            }
                        }
                    }
                }
                else
                {
                    GoToAttakPath(world, self, game);
                }
            }

            prevpos = new Vector(self.X, self.Y);
        }

        void SelectLine(World world, Wizard self, Game game)
        {
            int topcount = 0;
            int midcount = 0;
            int botcount = 0;
            Wizard[] wizards = world.Wizards;
            for (int i = 0; i < wizards.Length; i++)
            {
                if (wizards[i].Faction != enemyFaction)
                {
                    if (wizards[i].X > 800 || wizards[i].Y < world.Height - 800)
                    {
                        if (TopFilter(new Vector(wizards[i].X, wizards[i].Y), ref world))
                        {
                            topcount++;
                        }
                        else if (BotFilter(new Vector(wizards[i].X, wizards[i].Y), ref world))
                        {
                            botcount++;
                        }
                        else if (MidFilter(new Vector(wizards[i].X, wizards[i].Y), ref world))
                        {
                            midcount++;
                        }
                    }
                }
            }


            int mincount = Math.Min(topcount, Math.Min(midcount, botcount));
            if(mincount == midcount)
            {
                myline = LaneType.Middle;
                filter = MidFilter;
                attackpath = new Vector[5];
                attackpath[0] = new Vector(100, world.Height - 100);
                attackpath[1] = new Vector(400, world.Height - 400);
                attackpath[2] = new Vector(world.Width * 0.5, world.Height * 0.5);
                attackpath[3] = new Vector(world.Width - 400, 400);
                attackpath[4] = new Vector(world.Width - 200, 200);

                absmove = new AbstractMove();
                absmove.type = AbstractMoveType.GoTo;
                absmove.target = attackpath[3];

                MyCircularUnit unit = new MyCircularUnit(self);
                //unit.radius += 2;
                path = FindPath(new Vector(self.X, self.Y), attackpath[2], new MyWorld(world, self), 12, unit);
                absmove.target = attackpath[3];
                absmove.type = AbstractMoveType.GoTo;
                absmove.changeWithEnemiesInCastRange = true;
                absmove.nextType = AbstractMoveType.StendUp;
                pospath = 0;
            }
            else if(mincount == topcount)
            {
                myline = LaneType.Top;
                filter = TopFilter;
                attackpath = new Vector[6];
                attackpath[0] = new Vector(100, world.Height - 100);
                attackpath[1] = new Vector(100, world.Height - 800);
                attackpath[2] = new Vector(100, world.Height * 0.5);
                attackpath[3] = new Vector(200, 400);
                attackpath[4] = new Vector(world.Width * 0.5, 200);
                attackpath[5] = new Vector(world.Width - 200, 200);

                absmove = new AbstractMove();
                absmove.type = AbstractMoveType.GoTo;
                absmove.target = attackpath[3];

                MyCircularUnit unit = new MyCircularUnit(self);
                //unit.radius += 2;
                path = FindPath(new Vector(self.X, self.Y), attackpath[3], new MyWorld(world, self), 12, unit);
                absmove.target = attackpath[3];
                absmove.type = AbstractMoveType.GoTo;
                absmove.changeWithEnemiesInCastRange = true;
                absmove.nextType = AbstractMoveType.StendUp;
                pospath = 0;
            }
            else
            {
                myline = LaneType.Bottom;
                filter = BotFilter;
                attackpath = new Vector[6];
                attackpath[0] = new Vector(100, world.Height - 100);
                attackpath[1] = new Vector(800, world.Height - 100);
                attackpath[2] = new Vector(world.Width * 0.5, world.Height - 100);
                attackpath[3] = new Vector(world.Width - 400, world.Height - 200);
                attackpath[4] = new Vector(world.Width - 200, world.Height * 0.5);
                attackpath[5] = new Vector(world.Width - 200, 200);

                absmove = new AbstractMove();
                absmove.type = AbstractMoveType.GoTo;
                absmove.target = attackpath[3];

                MyCircularUnit unit = new MyCircularUnit(self);
                //unit.radius += 2;
                path = FindPath(new Vector(self.X, self.Y), attackpath[3], new MyWorld(world, self), 12, unit);
                absmove.target = attackpath[3];
                absmove.type = AbstractMoveType.GoTo;
                absmove.changeWithEnemiesInCastRange = true;
                absmove.nextType = AbstractMoveType.StendUp;
                pospath = 0;
            }
            
        }

        bool TopFilter(Vector pos, ref World world)
        {
            return pos.x < 800 || pos.y < 800;
        }

        bool BotFilter(Vector pos, ref World world)
        {
            return (pos.x > world.Width - 800) || (pos.y > world.Height - 800);
        }

        bool MidFilter(Vector pos, ref World world)
        {
            return Math.Abs(pos.x - pos.y) < 400;
        }

        void GoToAttakPath(World world, Wizard self, Game game)
        {
            int attackpathIndx = AttackPathIndex( self);

            if (attackpathIndx < attackpath.Length - 1)
                attackpathIndx++;

            absmove.type = AbstractMoveType.GoTo;
            absmove.target = attackpath[attackpathIndx];
            path = FindPath(new Vector(self.X, self.Y), attackpath[attackpathIndx], new MyWorld(world, self), 9, new MyCircularUnit(self));
            pospath = 0;
        }

        int AttackPathIndex( Unit self)
        {
            double mindist;
            int attackpathIndx = 0;
            mindist = Vector.Distance(self, attackpath[0]);
            for (int i = 1; i < attackpath.Length; i++)
            {
                double dist = Vector.Distance(self, attackpath[i]);
                if (dist < mindist)
                {
                    mindist = dist;
                    attackpathIndx = i;
                }
            }
            return attackpathIndx;
        }

        int AttackPathIndex(MyUnit self)
        {
            double mindist;
            int attackpathIndx = 0;
            mindist = Vector.Distance(self.pos, attackpath[0]);
            for (int i = 1; i < attackpath.Length; i++)
            {
                double dist = Vector.Distance(self.pos, attackpath[i]);
                if (dist < mindist)
                {
                    mindist = dist;
                    attackpathIndx = i;
                }
            }
            return attackpathIndx;
        }

        bool NearestI(World world, Unit self, Game game)
        {
            CircularUnit nearestenemy = null;
            double mindist = double.MaxValue;
            for (int i = 1; i < world.Minions.Length; i++)
            {
                if (world.Minions[i].Faction == enemyFaction)
                {
                    if (nearestenemy != null)
                    {
                        double dist = Vector.Distance(self, world.Minions[i]);
                        if (mindist > dist)
                        {
                            mindist = dist;
                            nearestenemy = world.Minions[i];
                        }
                    }
                    else
                    {
                        nearestenemy = world.Minions[i];
                        mindist = Vector.Distance(nearestenemy, self);
                    }
                }
            }

            for (int i = 0; i < world.Wizards.Length; i++)
            {
                if (world.Wizards[i].Faction == enemyFaction)
                {
                    if (nearestenemy != null)
                    {
                        double dist = Vector.Distance(self, world.Wizards[i]);
                        if (mindist > dist)
                        {
                            mindist = dist;
                            nearestenemy = world.Wizards[i];
                        }
                    }
                    else
                    {
                        nearestenemy = world.Wizards[i];
                        mindist = Vector.Distance(nearestenemy, self);
                    }
                }
            }

            for (int i = 0; i < world.Buildings.Length; i++)
            {
                if (world.Buildings[i].Faction == enemyFaction)
                {
                    if (nearestenemy != null)
                    {

                        double dist = Vector.Distance(self, world.Buildings[i]);
                        if (mindist > dist)
                        {
                            mindist = dist;
                            nearestenemy = world.Buildings[i];
                        }
                    }
                    else
                    {
                        nearestenemy = world.Buildings[i];
                        mindist = Vector.Distance(nearestenemy, self);
                    }
                }
            }

            if (nearestenemy != null)
            {
                mindist = Vector.Distance(self, nearestenemy);
                bool nearIsI = true;
                for (int i = 0; i < world.Minions.Length; i++)
                {
                    if (world.Minions[i].Faction == self.Faction)
                    {
                        double dist = Vector.Distance(world.Minions[i], nearestenemy);
                        if (dist < mindist)
                        {
                            return false;
                        }
                    }
                }

                for (int i = 0; i < world.Buildings.Length; i++)
                {
                    if (world.Buildings[i].Faction == self.Faction)
                    {
                        double dist = Vector.Distance(world.Buildings[i], nearestenemy);
                        if (dist < mindist)
                        {
                            return false;
                        }
                    }
                }
                
                if (nearIsI)
                {
                    return true;
                }
                else return false;
            }
            else return false;
        }

        bool NearestI(MyWorld world, MyUnit self, Game game)
        {
            MyCircularUnit nearestenemy = null;
            double mindist = double.MaxValue;
            for (int i = 1; i < world.minions.Length; i++)
            {
                if (world.minions[i].faction == enemyFaction)
                {
                    if (nearestenemy != null)
                    {
                        double dist = Vector.Distance(self.pos, world.minions[i].pos);
                        if (mindist > dist)
                        {
                            mindist = dist;
                            nearestenemy = world.minions[i];
                        }
                    }
                    else
                    {
                        nearestenemy = world.minions[i];
                        mindist = Vector.Distance(nearestenemy.pos, self.pos);
                    }
                }
            }

            for (int i = 0; i < world.wizards.Length; i++)
            {
                if (world.wizards[i].faction == enemyFaction)
                {
                    if (nearestenemy != null)
                    {
                        double dist = Vector.Distance(self, world.wizards[i]);
                        if (mindist > dist)
                        {
                            mindist = dist;
                            nearestenemy = world.wizards[i];
                        }
                    }
                    else
                    {
                        nearestenemy = world.wizards[i];
                        mindist = Vector.Distance(nearestenemy, self);
                    }
                }
            }

            for (int i = 0; i < world.buildings.Length; i++)
            {
                if (world.buildings[i].faction == enemyFaction)
                {
                    if (nearestenemy != null)
                    {

                        double dist = Vector.Distance(self, world.buildings[i]);
                        if (mindist > dist)
                        {
                            mindist = dist;
                            nearestenemy = world.buildings[i];
                        }
                    }
                    else
                    {
                        nearestenemy = world.buildings[i];
                        mindist = Vector.Distance(nearestenemy, self);
                    }
                }
            }

            if (nearestenemy != null)
            {
                mindist = Vector.Distance(self, nearestenemy);
                bool nearIsI = true;
                for (int i = 0; i < world.minions.Length; i++)
                {
                    if (world.minions[i].faction == self.faction)
                    {
                        double dist = Vector.Distance(world.minions[i], nearestenemy);
                        if (dist < mindist)
                        {
                            return false;
                        }
                    }
                }

                for (int i = 0; i < world.buildings.Length; i++)
                {
                    if (world.buildings[i].faction == self.faction)
                    {
                        double dist = Vector.Distance(world.buildings[i], nearestenemy);
                        if (dist < mindist)
                        {
                            return false;
                        }
                    }
                }

                if (nearIsI)
                {
                    return true;
                }
                else return false;
            }
            else return false;
        }

        bool FollowPath(Vector[] path, ref int pos, Wizard self, World world, Game game, Move move)
        {
            //���� �����
            LivingUnit nearestenemy = null;
            double mindist = double.MaxValue;
            for (int i = 1; i < world.Minions.Length; i++)
            {
                if (world.Minions[i].Faction == enemyFaction)
                {
                    if (nearestenemy != null)
                    {
                        double dist = Vector.Distance(self, world.Minions[i]);
                        if (mindist > dist)
                        {
                            mindist = dist;
                            nearestenemy = world.Minions[i];
                        }
                    }
                    else
                    {
                        nearestenemy = world.Minions[i];
                        mindist = Vector.Distance(nearestenemy, self);
                    }
                }
            }

            for (int i = 0; i < world.Wizards.Length; i++)
            {
                if (world.Wizards[i].Faction == enemyFaction)
                {
                    if (nearestenemy != null)
                    {
                        double dist = Vector.Distance(self, world.Wizards[i]);
                        if (mindist > dist)
                        {
                            mindist = dist;
                            nearestenemy = world.Wizards[i];
                        }
                    }
                    else
                    {
                        nearestenemy = world.Wizards[i];
                        mindist = Vector.Distance(nearestenemy, self);
                    }
                }
            }

            for (int i = 0; i < world.Buildings.Length; i++)
            {
                if (world.Buildings[i].Faction == enemyFaction)
                {
                    if (nearestenemy != null)
                    {

                        double dist = Vector.Distance(self, world.Buildings[i]);
                        if (mindist > dist)
                        {
                            mindist = dist;
                            nearestenemy = world.Buildings[i];
                        }
                    }
                    else
                    {
                        nearestenemy = world.Buildings[i];
                        mindist = Vector.Distance(nearestenemy, self);
                    }
                }
            }
            if (nearestenemy != null)
            {
                if (Vector.Distance(self, nearestenemy) <= game.WizardCastRange)
                {
                    double angle = self.GetAngleTo(nearestenemy);
                    move.Turn = angle;
                    if (Math.Abs(angle) <= game.StaffSector / 2)
                    {
                        move.Action = ActionType.MagicMissile;
                        move.CastAngle = angle;
                        move.MinCastDistance = Vector.Distance(self, nearestenemy) - nearestenemy.Radius + game.MagicMissileRadius;
                    }
                }
            }
            //����� ���� �����

            if (path.Length == 1)
            {
                return true;
            }
            else
            {
                if (Vector.Distance(path[pos + 1], new Vector(self.X, self.Y)) < 1)
                {
                    pos++;
                    if (pos + 1 == path.Length)
                        return true;
                }
                Vector next = path[pos + 1] - new Vector(self.X, self.Y);
                Vector forward = Vector.Forward(self);
                Vector right = new Vector(-forward.y, forward.x);
                move.Speed = next * forward;
                move.StrafeSpeed = next * right;
                return false;
            }
        }

        //void DrawVectorPath(Vector[] path, int scale, Vector zeropos, World world, MyCircularUnit unit)
        //{
        //    Bitmap map = new Bitmap(scale, scale);
        //    Graphics gr = Graphics.FromImage(map);
        //    gr.Clear(Color.White);
        //    Pen p = new Pen(Brushes.Black);
        //    Brush br = Brushes.Black;
        //    gr.FillEllipse(Brushes.Blue, (int)(unit.pos.x - zeropos.x - unit.radius), (int)(unit.pos.y - zeropos.y - unit.radius), (int)(2 * unit.radius), (int)(2 * unit.radius));
        //    gr.FillEllipse(Brushes.White, (int)(unit.pos.x - zeropos.x - 3), (int)(unit.pos.y - zeropos.y - 3), (int)(2 * 3), (int)(2 * 3));
        //    for (int i = 1; i < path.Length; i++)
        //    {
        //        int x1 = (int)(path[i - 1].x - zeropos.x);
        //        int y1 = (int)(path[i - 1].y - zeropos.y);
        //        int x2 = (int)(path[i].x - zeropos.x);
        //        int y2 = (int)(path[i].y - zeropos.y);

        //        gr.DrawLine(p, x1, y1, x2, y2);
        //        gr.FillRectangle(br, x1 - 1, y1 - 1, 3, 3);
        //    }

        //    br = Brushes.Red;
        //    for (int i = 0; i < world.Buildings.Length; i++)
        //    {
        //        int x = (int)(world.Buildings[i].X - zeropos.x);
        //        int y = (int)(world.Buildings[i].Y - zeropos.y);
        //        int radius = (int)(world.Buildings[i].Radius);
        //        gr.FillEllipse(br, x - radius, y - radius, 2 * radius, 2 * radius);
        //    }


        //    draw.Image = map;
        //    draw.Update();
        //}

        bool GoodPath(Wizard self, int cpos)
        {
            
            if (Vector.Distance(Vector.Position(self), bestcell.wizardstate[cpos].pos) > 10)
                return false;
            if (Math.Abs(self.Angle - bestcell.wizardstate[cpos].angle) > 0.03)
                return false;


            return true;
        }

        //void DebugWriteLine(string text)
        //{
        //    wr.WriteLine(text);
        //    Console.WriteLine(text);
        //}

        //�������� 2 �������
        //int ticks = 180;
        //int capcity = 100;
        //int mincapcity = 5;
        //int generations = 100;
        // 180 * 20 = 3,6 c

        int ticks = 60;
        int capcity = 10;
        int mincapcity = 5;
        int generations = 1000;
        Cell[] cells;

        Cell MakeTree(Wizard self, World world, Game game)
        {
            Cell.game = game;
            Cell.ticks = ticks;
            cells = new Cell[capcity];
            for (int i = 0;i < capcity;i++)
            {
                cells[i] = new Cell();
                //MakePrice(ref cells[i], ref world, ref self, ref game, false);
            }
            
            for(int i = 0;i < generations;i++)
            {
                GeneStep(ref world, ref self, ref game);
            }

            //MakePrice(ref cells[0], ref world, ref self, ref game, true);
            return cells[0];
        }

        

        

        bool Collide(MyCircularUnit u1, MyCircularUnit u2)
        {
            if (Vector.Distance(u1.pos, u2.pos) < u1.radius + u2.radius)
                return true;
            else return false;
        }

        void GeneStep(ref World w, ref Wizard self, ref Game game)
        {
            quickSort(cells, 0, cells.Length - 1);
            for(int i = mincapcity;i < capcity;i++)
            {
                cells[i].Regen(cells[r.Next(capcity)]);
                //MakePrice(ref cells[i], ref w, ref self, ref game, false);
            }
        }

        ~MyStrategy()
        {
            //if (debug)
            //{
            //    wr.Close();
            //}
        }
        Vector[] RunOut(Vector from, Faction faction, MyWorld world, double step, MyCircularUnit unit, int range)
        {
            double startdist = MinRangeFromFaction(from, world, unit.faction);
            int xsteps = (int)(world.Width / step) + 1;
            int ysteps = (int)(world.Height / step) + 1;
            int[,] waveMap = new int[xsteps, ysteps];
            int xstart = (int)(from.x / step);
            int ystart = (int)(from.y / step);
            double dx = from.x - xstart * step;
            double dy = from.y - ystart * step;
            int xend = 0;
            int yend = 0;
            waveMap[xstart, ystart] = 1;
            int stage = 1;
            int dxsteps = xsteps - 1;
            int dysteps = ysteps - 1;
            Vector ppos = new Vector();
            int interations = 0;
            int pathIndex = AttackPathIndex(unit);
            double pathdist = 0;
            bool backmove = false;

            if(pathIndex > 0)
            {
                backmove = true;
                pathIndex--;
                pathdist = Vector.Distance(from, attackpath[pathIndex]);
            }
            Vector pathpos = attackpath[pathIndex];

            do
            {
                interations = 0;
                world.MakeMove(new Move(), (int)(step / 3));
                int stage2 = stage + 1;
                for (int x = 0; x < xsteps; x++)
                {
                    for (int y = 0; y < ysteps; y++)
                    {
                        if (stage == waveMap[x, y])
                        {
                            if (x > 0)
                            {
                                if (waveMap[x - 1, y] == 0)
                                {
                                    unit.pos.x = (x - 1) * step + dx;
                                    unit.pos.y = y * step + dy;
                                    if (!world.TestCollide(unit))
                                    {
                                        interations++;
                                        waveMap[x - 1, y] = stage2;
                                        ppos.x = (x - 1) * step + dx;
                                        ppos.y = y * step + dy;
                                        if (MinRangeFromFaction(ppos, world, faction) >= range)
                                        {
                                            if(backmove)
                                            {
                                                if(Vector.Distance(ppos,pathpos) < pathdist)
                                                {

                                                    xend = x - 1;
                                                    yend = y;
                                                    goto fationout;
                                                }
                                            }
                                            else
                                            {

                                                xend = x - 1;
                                                yend = y;
                                                goto fationout;
                                            }
                                        }
                                    }
                                }
                            }
                            if (x < dxsteps)
                            {
                                if (waveMap[x + 1, y] == 0)
                                {
                                    unit.pos.x = (x + 1) * step + dx;
                                    unit.pos.y = y * step + dy;
                                    if (!world.TestCollide(unit))
                                    {
                                        interations++;
                                        waveMap[x + 1, y] = stage2;
                                        ppos.x = (x + 1) * step + dx;
                                        ppos.y = y * step + dy;
                                        if (MinRangeFromFaction(ppos, world, faction) >= range)
                                        {
                                            if (backmove)
                                            {
                                                if (Vector.Distance(ppos, pathpos) < pathdist)
                                                {

                                                    xend = x + 1;
                                                    yend = y;
                                                    goto fationout;
                                                }
                                            }
                                            else
                                            {

                                                xend = x + 1;
                                                yend = y;
                                                goto fationout;
                                            }

                                        }
                                    }
                                }
                            }

                            if (y > 0)
                            {
                                if (waveMap[x, y - 1] == 0)
                                {
                                    unit.pos.x = x * step + dx;
                                    unit.pos.y = (y - 1) * step + dy;
                                    if (!world.TestCollide(unit))
                                    {
                                        interations++;
                                        waveMap[x, y - 1] = stage2;
                                        ppos.x = x * step + dx;
                                        ppos.y = (y - 1) * step + dy;
                                        if (MinRangeFromFaction(ppos, world, faction) >= range)
                                        {
                                            if (backmove)
                                            {
                                                if (Vector.Distance(ppos, pathpos) < pathdist)
                                                {

                                                    xend = x;
                                                    yend = y - 1;
                                                    goto fationout;
                                                }
                                            }
                                            else
                                            {

                                                xend = x;
                                                yend = y - 1;
                                                goto fationout;
                                            }

                                        }
                                    }
                                }
                            }
                            if (y < dysteps)
                            {
                                if (waveMap[x, y + 1] == 0)
                                {
                                    unit.pos.x = x * step + dx;
                                    unit.pos.y = (y + 1) * step + dy;
                                    if (!world.TestCollide(unit))
                                    {
                                        interations++;
                                        waveMap[x, y + 1] = stage2;
                                        ppos.x = x * step + dx;
                                        ppos.y = (y + 1) * dy + step;
                                        if (MinRangeFromFaction(ppos, world, faction) >= range)
                                        {
                                            if (backmove)
                                            {
                                                if (Vector.Distance(ppos, pathpos) < pathdist)
                                                {
                                                    xend = x;
                                                    yend = y + 1;
                                                    goto fationout;
                                                }
                                            }
                                            else
                                            {

                                                xend = x;
                                                yend = y + 1;
                                                goto fationout;
                                            }

                                        }
                                    }

                                }
                            }
                        }
                    }
                }
                stage = stage2;
            } while (interations > 0);
            if(interations == 0)
            {
                return new Vector[] { new Vector(0, 0) };
            }

            fationout:
            Vector[] path = new Vector[waveMap[xend, yend]];
            int xnow = xend;
            int ynow = yend;
            do
            {
                int dval = waveMap[xnow, ynow] - 1;
                path[dval] = new Vector(xnow * step + dx, ynow * step + dy);
                if (xnow > 0)
                {
                    if (waveMap[xnow - 1, ynow] == dval)
                    {
                        xnow--;
                        continue;
                    }
                }
                if (xnow < dxsteps)
                {
                    if (waveMap[xnow + 1, ynow] == dval)
                    {
                        xnow++;
                        continue;
                    }
                }
                if (ynow > 0)
                {
                    if (waveMap[xnow, ynow - 1] == dval)
                    {
                        ynow--;
                        continue;
                    }
                }
                if (ynow < dysteps)
                {
                    if (waveMap[xnow, ynow + 1] == dval)
                    {
                        ynow++;
                        continue;
                    }
                }
            } while (xnow != xstart || ynow != ystart);
            path[0] = new Vector(xstart * step + dx, ystart * step + dy);

            return path;
        }

        Vector[] RunIn(Vector from, Faction faction, MyWorld world, double step, MyCircularUnit unit, int range)
        {
            int xsteps = (int)(world.Width / step) + 1;
            int ysteps = (int)(world.Height / step) + 1;
            int[,] waveMap = new int[xsteps, ysteps];
            int xstart = (int)(from.x / step);
            int ystart = (int)(from.y / step);
            double dx = from.x - xstart * step;
            double dy = from.y - ystart * step;
            int xend = 0;
            int yend = 0;
            waveMap[xstart, ystart] = 1;
            int stage = 1;
            int dxsteps = xsteps - 1;
            int dysteps = ysteps - 1;
            Vector ppos = new Vector();
            int interactions = 0;
            do
            {
                interactions = 0;
                world.MakeMove(new Move(), (int)(step / 3));
                int stage2 = stage + 1;
                for (int x = 0; x < xsteps; x++)
                {
                    for (int y = 0; y < ysteps; y++)
                    {
                        if (stage == waveMap[x, y])
                        {
                            interactions++;
                            if (x > 0)
                            {
                                if (waveMap[x - 1, y] == 0)
                                {
                                    unit.pos.x = (x - 1) * step + dx;
                                    unit.pos.y = y * step + dy;
                                    if (!world.TestCollide(unit))
                                    {
                                        waveMap[x - 1, y] = stage2;
                                        ppos.x = (x - 1) * step + dx;
                                        ppos.y = y * step + dy;
                                        if (MinRangeFromFaction(ppos, world, faction) <= range)
                                        {
                                            xend = x - 1;
                                            yend = y;
                                            goto fationout;
                                        }
                                    }
                                }
                            }
                            if (x < dxsteps)
                            {
                                if (waveMap[x + 1, y] == 0)
                                {
                                    unit.pos.x = (x + 1) * step + dx;
                                    unit.pos.y = y * step + dy;
                                    if (!world.TestCollide(unit))
                                    {
                                        waveMap[x + 1, y] = stage2;
                                        ppos.x = (x + 1) * step + dx;
                                        ppos.y = y * step + dy;
                                        if (MinRangeFromFaction(ppos, world, faction) <= range)
                                        {
                                            xend = x + 1;
                                            yend = y;
                                            goto fationout;
                                        }
                                    }
                                }
                            }

                            if (y > 0)
                            {
                                if (waveMap[x, y - 1] == 0)
                                {
                                    unit.pos.x = x * step + dx;
                                    unit.pos.y = (y - 1) * step + dy;
                                    if (!world.TestCollide(unit))
                                    {
                                        waveMap[x, y - 1] = stage2;
                                        ppos.x = x * step + dx;
                                        ppos.y = (y - 1) * step + dy;
                                        if (MinRangeFromFaction(ppos, world, faction) <= range)
                                        {
                                            xend = x;
                                            yend = y - 1;
                                            goto fationout;
                                        }
                                    }
                                }
                            }
                            if (y < dysteps)
                            {
                                if (waveMap[x, y + 1] == 0)
                                {
                                    unit.pos.x = x * step + dx;
                                    unit.pos.y = (y + 1) * step + dy;
                                    if (!world.TestCollide(unit))
                                    {
                                        waveMap[x, y + 1] = stage2;
                                        ppos.x = x * step + dx;
                                        ppos.y = (y + 1) * step + dy;
                                        if (MinRangeFromFaction(ppos, world, faction) <= range)
                                        {
                                            xend = x;
                                            yend = y + 1;
                                            goto fationout;
                                        }
                                    }

                                }
                            }
                        }
                    }
                }
                stage = stage2;
            } while (interactions > 0);
            if(interactions == 0)
            {
                return new Vector[] { from };
            }
        fationout:
            Vector[] path = new Vector[waveMap[xend, yend]];
            int xnow = xend;
            int ynow = yend;
            do
            {
                int dval = waveMap[xnow, ynow] - 1;
                path[dval] = new Vector(xnow * step + dx, ynow * step + dy);
                if (xnow > 0)
                {
                    if (waveMap[xnow - 1, ynow] == dval)
                    {
                        xnow--;
                        continue;
                    }
                }
                if (xnow < dxsteps)
                {
                    if (waveMap[xnow + 1, ynow] == dval)
                    {
                        xnow++;
                        continue;
                    }
                }
                if (ynow > 0)
                {
                    if (waveMap[xnow, ynow - 1] == dval)
                    {
                        ynow--;
                        continue;
                    }
                }
                if (ynow < dysteps)
                {
                    if (waveMap[xnow, ynow + 1] == dval)
                    {
                        ynow++;
                        continue;
                    }
                }
            } while (xnow != xstart || ynow != ystart);
            path[0] = new Vector(xstart * step + dx, ystart * step + dy);

            return path;
        }

        double MinRangeFromFaction(Vector pos, MyWorld world, Faction faction)
        {
            double mindist = double.MaxValue;
            for(int i = 0;i < world.buildings.Length;i++)
            {
                if(world.buildings[i].faction == faction)
                    mindist = Math.Min(Vector.Distance(pos, world.buildings[i].pos), mindist);
            }
            for (int i = 0; i < world.wizards.Length; i++)
            {
                if (world.wizards[i].faction == faction)
                    mindist = Math.Min(Vector.Distance(pos, world.wizards[i].pos), mindist);
            }
            for (int i = 0; i < world.minions.Length; i++)
            {
                if (world.minions[i].faction == faction)
                    mindist = Math.Min(Vector.Distance(pos, world.minions[i].pos), mindist);
            }
            return mindist;
        }
        Vector[] FindPath(Vector from, Vector to, MyWorld world, double step, MyCircularUnit unit)
        {
            int xsteps = (int)(world.Width / step) + 1;
            int ysteps = (int)(world.Height / step) + 1;
            int[,] waveMap = new int[xsteps, ysteps];
            int xstart = (int)(from.x / step);
            int ystart = (int)(from.y / step);
            double dx = from.x - xstart * step;
            double dy = from.y - ystart * step;
            int xend = (int)(to.x / step);
            int yend = (int)(to.y / step);
            waveMap[xstart, ystart] = 1;
            int stage = 1;
            int dxsteps = xsteps - 1;
            int dysteps = ysteps - 1;
            int interacions = 0;
            do
            {
                interacions = 0;
                world.MakeMove(new Move(), (int)(step / 3));
                int stage2 = stage + 1;
                for(int x = 0;x < xsteps;x++)
                {
                    for(int y = 0;y < ysteps;y++)
                    { 
                        if(stage == waveMap[x,y])
                        {
                            if(x > 0)
                            {
                                if (waveMap[x - 1, y] == 0)
                                {
                                    unit.pos.x = (x - 1) * step + dx;
                                    unit.pos.y = y * step + dy;
                                    if (!world.TestCollide(unit))
                                    {
                                        interacions++;
                                        waveMap[x - 1, y] = stage2;
                                    }
                                }
                            }
                            if (x < dxsteps)
                            {
                                if (waveMap[x + 1, y] == 0)
                                {
                                    unit.pos.x = (x + 1) * step + dx;
                                    unit.pos.y = y * step + dy;
                                    if (!world.TestCollide(unit))
                                    {
                                        interacions++;
                                        waveMap[x + 1, y] = stage2;
                                    }
                                }
                            }

                            if (y > 0)
                            {
                                if (waveMap[x, y - 1] == 0)
                                {
                                    unit.pos.x = x * step + dx;
                                    unit.pos.y = (y - 1) * step + dy;
                                    if (!world.TestCollide(unit))
                                    {
                                        interacions++;
                                        waveMap[x, y - 1] = stage2;
                                    }
                                    else
                                    {
                                        ;
                                    }
                                }
                            }
                            if (y < dysteps)
                            {
                                if (waveMap[x, y + 1] == 0)
                                {
                                    unit.pos.x = x * step + dx;
                                    unit.pos.y = (y + 1) * step + dy;
                                    if (!world.TestCollide(unit))
                                    {
                                        interacions++;
                                        waveMap[x, y + 1] = stage2;
                                    }

                                }
                            }
                        }
                    }
                }
                stage = stage2;
            } while (waveMap[xend, yend] == 0 && interacions > 0);
            if(interacions == 0)
            {
                return new Vector[] { to };
            }

            Vector[] path = new Vector[waveMap[xend, yend]];
            if(path.Length == 1)
            {
                path[0] = new Vector(xend * step + dx, yend * step + dy);
                return path;
            }
            int xnow = xend;
            int ynow = yend;
            do
            {
                int dval = waveMap[xnow,ynow] - 1;
                path[dval] = new Vector(xnow * step + dx, ynow * step + dy);
                if(xnow > 0)
                {
                    if(waveMap[xnow - 1,ynow] == dval)
                    {
                        xnow--;
                        continue;
                    }
                }
                if(xnow < dxsteps)
                {
                    if(waveMap[xnow + 1,ynow] == dval)
                    {
                        xnow++;
                        continue;
                    }
                }
                if (ynow > 0)
                {
                    if (waveMap[xnow, ynow - 1] == dval)
                    {
                        ynow--;
                        continue;
                    }
                }
                if (ynow < dysteps)
                {
                    if (waveMap[xnow, ynow + 1] == dval)
                    {
                        ynow++;
                        continue;
                    }
                }
            } while (xnow != xstart || ynow != ystart);
            path[0] = new Vector(xstart * step + dx, ystart * step + dy);

            return path;
        }

        static void quickSort(Cell[] a, int l, int r)
        {
            Cell temp;
            Cell x = a[l + (r - l) / 2];
            //������ ������������ (l+r)/2, 
            //�� �� �������� ������������ �� ������� ������
            int i = l;
            int j = r;
            //��� � while ������ ������� � ��������� particle
            while (i <= j)
            {
                while (a[i] < x) i++;
                while (a[j] > x) j--;
                if (i <= j)
                {
                    temp = a[i];
                    a[i] = a[j];
                    a[j] = temp;
                    i++;
                    j--;
                }
            }
            if (i < r)
                quickSort(a, i, r);

            if (l < j)
                quickSort(a, l, j);
        }
    }

    enum UltraAbstractMoveType { DefendBuilding, HardDefentBuilding}
    class UltraAbstractMove
    {
        public UltraAbstractMoveType type;
    }
    enum AbstractMoveType {GoTo, StendUp }
    class AbstractMove
    {
        public AbstractMoveType type;
        public Vector target;
        public long IdTarget;
        public bool changeWithEnemiesInCastRange = false;
        public AbstractMoveType nextType;
    }

    class Cell
    {
        
        public static Game game;
        public static Random r = new Random();
        public static int step = 2;
        public static int ticks;
        public Move[] moves;
        public MyWizard[] wizardstate;
        public double points;

        public Cell()
        {
            moves = new Move[ticks / step];

            for(int i = 0;i < moves.Length;i++)
            {
                Move m = new Move();
                m.Action = (ActionType)r.Next(3);
                m.MinCastDistance = r.Next(100, 600);
                int rand = r.Next(4);
                if (rand <= 1)
                    m.Speed = game.WizardForwardSpeed;
                else if (rand == 2)
                    m.Speed = 0;
                else m.Speed = -game.WizardBackwardSpeed;
                m.Turn = (r.NextDouble() - 0.5) * 2 * game.WizardMaxTurnAngle;
                moves[i] = m;
            }
        }
        public void Regen(Cell c)
        {
            int ir = r.Next(c.moves.Length);
            for (int i = 0; i < ir; i++)
            {
                moves[i] = c.moves[i];
            }
            for (int i = ir; i < c.moves.Length; i++)
            {
                Move m = new Move();
                m.Action = (ActionType)r.Next(3);
                m.MinCastDistance = r.Next(100, 600);
                int rand = r.Next(3);
                if (rand == 0)
                    m.Speed = game.WizardForwardSpeed;
                else if (rand == 1)
                    m.Speed = 0;
                else m.Speed = -game.WizardBackwardSpeed;
                m.Turn = (r.NextDouble() - 0.5) * 2 * game.WizardMaxTurnAngle;
                moves[i] = m;
            }
            points = 0;
        }
        public void Regen(Cell c1, Cell c2)
        {
            int ir = r.Next(c1.moves.Length);
            for(int i = 0;i < ir;i++)
            {
                moves[i] = c1.moves[i];
            }
            for (int i = ir; i < c1.moves.Length; i++)
            {
                moves[i] = c2.moves[i];
            }
            points = 0;
        }

        public static bool operator >(Cell c1, Cell c2)
        {
            return c1.points < c2.points;
        }
        public static bool operator <(Cell c1, Cell c2)
        {
            return c1.points > c2.points;
        }

        public override string ToString()
        {
            return points.ToString();
        }
    }


    class MyUnit
    {
        public Vector pos;
        public Vector speed;
        public double angle;
        public Faction faction;
        public long Id;

        public MyUnit()
        {
            pos = new Vector();
            speed = new Vector();
            angle = 0;
        }

        public MyUnit(Unit unit)
        {
            pos = Vector.Position(unit);
            speed = Vector.Speed(unit);
            angle = unit.Angle;
            faction = unit.Faction;
            Id = unit.Id;
        }
        public MyUnit(MyUnit unit)
        {
            pos = unit.pos * 1;
            speed = unit.speed * 1;
            angle = unit.angle;
            faction = unit.faction;
            Id = unit.Id;
        }
    }

    class MyProjectile : MyCircularUnit
    {
        public Vector startpos;
        public ProjectileType Type;
        public long OwnerPlayerId;
        public long OwnerUnitId;
        public double mincast;
        public MyProjectile(Projectile unit) : base(unit)
        {
            Type = unit.Type;
            OwnerPlayerId = unit.OwnerPlayerId;
            OwnerUnitId = unit.OwnerUnitId;
            mincast = 0;
            startpos = pos * 1;
        }

        public MyProjectile() : base()
        {

        }

        public static MyProjectile MagicMissle(MyWizard wizard)
        {
            MyProjectile proj = new MyProjectile();
            proj.OwnerPlayerId = wizard.OwnerPlayerId;
            proj.OwnerUnitId = wizard.Id;
            proj.pos = wizard.pos * 1;
            proj.angle = wizard.angle;
            proj.speed = Vector.Forward(proj.angle).Multiply(Cell.game.MagicMissileSpeed);
            proj.radius = Cell.game.MagicMissileRadius;
            proj.Type = ProjectileType.MagicMissile;
            proj.startpos = proj.pos * 1;
            return proj;
        }
    }

    class MyCircularUnit : MyUnit
    {
        public double radius;
        public MyCircularUnit() : base()
        {
            radius = 0;
        }

        public MyCircularUnit(CircularUnit unit) : base(unit)
        {
            radius = unit.Radius;
        }

        public MyCircularUnit(MyCircularUnit unit) : base(unit)
        {
            radius = unit.radius;
        }
    }

    class MyLivingUnit : MyCircularUnit
    {
        public Status[] Statuses;
        public int Life;
        public int MaxLife;
        public MyLivingUnit(LivingUnit unit) : base(unit)
        {
            Life = unit.Life;
            MaxLife = unit.MaxLife;
            Statuses = unit.Statuses;
        }

        public MyLivingUnit(MyLivingUnit unit) : base(unit)
        {
            Life = unit.Life;
            MaxLife = unit.MaxLife;
            Statuses = unit.Statuses;
        }
    }

    class MyWizard : MyLivingUnit
    {
        public double CastRange;
        public bool isMaster;
        public bool isMe;
        public int Level;
        public int Mana;
        public int MaxMana;
        public Message[] Messages;
        public long OwnerPlayerId;
        public int RemainingActionCooldownTicks;
        public int[] RemainingCooldownTicksByAction;
        public SkillType[] Skills;
        public int Xp;
        public MyWizard(Wizard unit) : base(unit)
        {
            CastRange = unit.CastRange;
            isMaster = unit.IsMaster;
            isMe = unit.IsMe;
            Level = unit.Level;
            Mana = unit.Mana;
            MaxMana = unit.MaxMana;
            Messages = unit.Messages;
            OwnerPlayerId = unit.OwnerPlayerId;
            RemainingActionCooldownTicks = unit.RemainingActionCooldownTicks;
            RemainingCooldownTicksByAction = unit.RemainingCooldownTicksByAction;
            Skills = unit.Skills;
            Xp = unit.Xp;
        }

        public MyWizard(MyWizard unit) : base(unit)
        {
            CastRange = unit.CastRange;
            isMaster = unit.isMaster;
            isMe = unit.isMaster;
            Level = unit.Level;
            Mana = unit.Mana;
            MaxMana = unit.MaxMana;
            Messages = unit.Messages;
            OwnerPlayerId = unit.OwnerPlayerId;
            RemainingActionCooldownTicks = unit.RemainingActionCooldownTicks;
            RemainingCooldownTicksByAction = unit.RemainingCooldownTicksByAction;
            Skills = unit.Skills;
            Xp = unit.Xp;
        }
    }

    class MyMinion : MyLivingUnit
    {
        public MinionType Type;
        public double VisionRange;
        public int Damage;
        public int CooldownTicks;
        public int RemainingActionCooldownTicks;

        public MyMinion(Minion unit) : base(unit)
        {
            Type = unit.Type;
            VisionRange = unit.VisionRange;
            Damage = unit.Damage;
            CooldownTicks = unit.CooldownTicks;
            RemainingActionCooldownTicks = unit.RemainingActionCooldownTicks;
        }
        
    }

    class MyTree : MyLivingUnit
    {
        public MyTree(Tree tree) : base(tree)
        {

        }
    }

    class MyBuilding : MyLivingUnit
    {
        public BuildingType Type;
        public double VisionRange;
        public double AttackRange;
        public int Damage;
        public int CooldownTicks;
        public int RemainingActionCooldownTicks;

        public MyBuilding(Building unit) : base(unit)
        {
            Type = unit.Type;
            VisionRange = unit.VisionRange;
            AttackRange = unit.AttackRange;
            Damage = unit.Damage;
            CooldownTicks = unit.CooldownTicks;
            RemainingActionCooldownTicks = unit.RemainingActionCooldownTicks;
        }
    }

    class MyWorld
    {
        public List<MyProjectile> projs;
        public MyMinion[] minions;
        public MyBuilding[] buildings;
        public MyTree[] trees;
        public MyWizard[] wizards;
        public List<MyCircularUnit>[,] collidebox;
        public MyWizard mywizard;
        public Faction enemyFaction;
        public int cstep = 200;
        public int cwidth;
        public int cheight;
        public World w;
        public double Width;
        public double Height;
        public MyWorld(World w, Wizard self)
        {
            Width = w.Width;
            Height = w.Height;
            this.w = w;
            mywizard = new MyWizard(self);
            
            if (mywizard.faction == Faction.Academy)
                enemyFaction = Faction.Renegades;
            else enemyFaction = Faction.Academy;

            projs = new List<MyProjectile>();
            Projectile[] wps = w.Projectiles;
            for (int i = 0; i < wps.Length; i++)
            {

                projs.Add(new MyProjectile(wps[i]));
            }

            Minion[] wms = w.Minions;
            minions = new MyMinion[wms.Length];
            for (int i = 0; i < wms.Length; i++)
            {
                minions[i] = new MyMinion(wms[i]);
            }
            Building[] wbs = w.Buildings;
            buildings = new MyBuilding[wbs.Length];
            for (int i = 0; i < wbs.Length; i++)
            {
                buildings[i] = new MyBuilding(wbs[i]);
            }
            Tree[] wts = w.Trees;
            trees = new MyTree[wts.Length];
            for (int i = 0; i < trees.Length; i++)
            {
                trees[i] = new MyTree(wts[i]);
            }
            Wizard[] wws = w.Wizards;
            wizards = new MyWizard[wws.Length];
            for (int i = 0; i < wws.Length; i++)
            {
                wizards[i] = new MyWizard(wws[i]);
            }
            
            
            cwidth = (int)(w.Width / cstep);
            cheight = (int)(int)(w.Height / cstep);
            collidebox = new List<MyCircularUnit>[cwidth, cheight];
            for (int x = 0; x < cwidth; x++)
            {
                for (int y = 0; y < cheight; y++)
                {
                    collidebox[x, y] = new List<MyCircularUnit>();
                }
            }
            for (int i = 0; i < trees.Length; i++)
            {
                MyTree tree = trees[i];
                int x = (int)(tree.pos.x / cstep);
                int y = (int)(tree.pos.y / cstep);
                collidebox[x, y].Add(tree);
            }
            for (int i = 0; i < minions.Length; i++)
            {
                MyMinion tree = minions[i];
                int x = (int)(tree.pos.x / cstep);
                int y = (int)(tree.pos.y / cstep);
                collidebox[x, y].Add(tree);
            }
            for (int i = 0; i < buildings.Length; i++)
            {
                MyBuilding tree = buildings[i];
                int x = (int)(tree.pos.x / cstep);
                int y = (int)(tree.pos.y / cstep);
                collidebox[x, y].Add(tree);
            }
            for (int i = 0; i < wizards.Length; i++)
            {
                MyWizard tree = wizards[i];
                if (!tree.isMe)
                {
                    int x = (int)(tree.pos.x / cstep);
                    int y = (int)(tree.pos.y / cstep);
                    collidebox[x, y].Add(tree);
                }
            }
        }

        public void MakeMove(Move m, int steps)
        {
            Game game = Cell.game;
            if (m.Action == ActionType.Staff)
            {
                if (mywizard.RemainingCooldownTicksByAction[1] == 0 && mywizard.RemainingActionCooldownTicks == 0)
                {
                    mywizard.RemainingActionCooldownTicks = 30;
                    mywizard.RemainingCooldownTicksByAction[1] = 60;
                    double cos = Math.Cos(Math.PI / 12);
                    double staffRange = game.StaffRange;
                    int staffDamage = game.StaffDamage;
                    for (int j = 0; j < buildings.Length; j++)
                    {
                        MyCircularUnit b = buildings[j];
                        if (Vector.Distance(mywizard.pos, b.pos) < staffRange + b.radius)
                        {
                            if (Vector.CosTo(mywizard, b) > cos)
                            {
                                if (buildings[j].Life > 0)
                                {
                                    buildings[j].Life -= staffDamage;
                                }
                            }
                        }
                    }

                    for (int j = 0; j < minions.Length; j++)
                    {
                        MyCircularUnit b = minions[j];
                        if (Vector.Distance(mywizard.pos, b.pos) < staffRange + b.radius)
                        {
                            if (Vector.CosTo(mywizard, b) > cos)
                            {
                                if (minions[j].Life > 0)
                                {
                                    minions[j].Life -= staffDamage;
                                }
                            }
                        }
                    }

                    for (int j = 0; j < wizards.Length; j++)
                    {
                        MyCircularUnit b = wizards[j];
                        if (Vector.Distance(mywizard.pos, b.pos) < staffRange + b.radius)
                        {
                            if (Vector.CosTo(mywizard, b) > cos)
                            {
                                if (wizards[j].Life > 0)
                                {
                                    wizards[j].Life -= staffDamage;
                                }
                            }
                        }
                    }
                }
            }
            else if (m.Action == ActionType.MagicMissile)
            {
                if (mywizard.RemainingCooldownTicksByAction[2] == 0 && mywizard.RemainingActionCooldownTicks == 0)
                {
                    if (mywizard.Mana > game.MagicMissileManacost)
                    {
                        mywizard.Mana -= game.MagicMissileManacost;
                        projs.Add(MyProjectile.MagicMissle(mywizard));
                        projs[projs.Count - 1].mincast = m.MinCastDistance;
                        mywizard.RemainingCooldownTicksByAction[2] = game.MagicMissileCooldownTicks;
                        mywizard.RemainingActionCooldownTicks = 30;
                    }
                }
            }
            for (int j = 0; j < steps; j++)
            {
                //��������� ��������
                for (int k = 0; k < minions.Length; k++)
                {
                    double dist = Vector.Distance(minions[k].pos, mywizard.pos);
                    //������ ��������� ������ �������� ������� � ����
                    if (dist <= 1000)
                    {
                        //����� minion ����� ������ ����� � ������ ���������
                        MyMinion min = minions[k];
                        int x = (int)(min.pos.x / cstep);
                        int y = (int)(min.pos.y / cstep);
                        List<MyCircularUnit> circs = collidebox[x, y];
                        for (int i = 0; i < circs.Count; i++)
                        {
                            if (circs[i].Id == min.Id)
                            {
                                circs.RemoveAt(i);
                                break;
                            }
                        }
                        min.pos += min.speed;
                        if (min.pos.x < min.radius)
                            min.pos.x = min.radius;
                        else if(min.pos.x > Width - min.radius)
                                min.pos.x = Width - min.radius;
                        if (min.pos.y < min.radius)
                            min.pos.y = min.radius;
                        else if(min.pos.y > Height - min.radius)
                        {
                            min.pos.y = Height - min.radius;
                        }
                        x = (int)(min.pos.x / cstep);
                        y = (int)(min.pos.y / cstep);
                        collidebox[x, y].Add(min);
                    }
                }

                //��������� �����������
                //����� �� ����������� ��� ���� ������ ��� D:
                for (int k = 0; k < wizards.Length; k++)
                {
                    MyWizard wiz = wizards[k];
                    double dist = Vector.Distance(wiz.pos, mywizard.pos);
                    if (wiz.faction == enemyFaction)
                    {
                        if (dist <= wiz.CastRange)
                        {
                            if (wiz.RemainingCooldownTicksByAction[2] == 0 && wiz.RemainingActionCooldownTicks == 0)
                            {
                                if (wiz.Mana >= game.MagicMissileManacost)
                                {
                                    wiz.Mana -= game.MagicMissileManacost;
                                    projs.Add(MyProjectile.MagicMissle(wiz));
                                    projs[projs.Count - 1].speed = (mywizard.pos - wiz.pos).Normalize() * game.MagicMissileSpeed;
                                    projs[projs.Count - 1].mincast = dist - mywizard.radius + game.MagicMissileRadius;
                                    mywizard.RemainingCooldownTicksByAction[2] = game.MagicMissileCooldownTicks;
                                    mywizard.RemainingActionCooldownTicks = 30;
                                }
                            }
                        }

                        wiz.RemainingActionCooldownTicks--;
                        if (wiz.RemainingActionCooldownTicks < 0)
                            wiz.RemainingActionCooldownTicks = 0;
                        for (int n = 0; n < wiz.RemainingCooldownTicksByAction.Length; n++)
                        {
                            wiz.RemainingCooldownTicksByAction[n]--;
                            if (wiz.RemainingCooldownTicksByAction[n] < 0)
                                wiz.RemainingCooldownTicksByAction[n] = 0;
                        }
                    }
                }

                Vector npos = mywizard.pos + Vector.Forward(mywizard) * m.Speed;
                bool collide = false;
                int xcol = (int)(npos.x / cstep);
                int ycol = (int)(npos.y / cstep);
                if (npos.x < mywizard.radius)
                {
                    mywizard.pos.x = mywizard.radius;
                    collide = true;
                }
                else if (npos.x > w.Width - mywizard.radius)
                {
                    mywizard.pos.x = w.Width - mywizard.radius;
                    collide = true;
                }
                if (npos.y < mywizard.radius)
                {
                    mywizard.pos.y = mywizard.radius;
                    collide = true;
                }
                else if (npos.y > w.Height - mywizard.radius)
                {
                    mywizard.pos.y = w.Height - mywizard.radius;
                    collide = true;
                }

                Vector forw = Vector.Forward(mywizard);
                List<MyCircularUnit> cols;
                //mywizard.pos = npos;
                if (!collide)
                {
                    cols = collidebox[xcol, ycol];
                    for (int k = 0; k < cols.Count; k++)
                    {
                        if (Collide(mywizard, cols[k]))
                        {
                            
                            collide = true;
                            break;
                        }
                    }
                }

                if (xcol > 0 && !collide)
                {
                    cols = collidebox[xcol - 1, ycol];
                    for (int k = 0; k < cols.Count; k++)
                    {
                        if (Collide(mywizard, cols[k]))
                        {
                            
                            collide = true;
                            break;
                        }
                    }
                }

                if (xcol < cwidth - 1 && !collide)
                {
                    cols = collidebox[xcol + 1, ycol];
                    for (int k = 0; k < cols.Count; k++)
                    {
                        if (Collide(mywizard, cols[k]))
                        {
                            
                            collide = true;
                            break;
                        }
                    }
                }

                if (ycol > 0 && !collide)
                {
                    cols = collidebox[xcol, ycol - 1];
                    for (int k = 0; k < cols.Count; k++)
                    {
                        if (Collide(mywizard, cols[k]))
                        {
                            
                            collide = true;
                            break;
                        }
                    }
                }

                if (ycol < cheight - 1 && !collide)
                {
                    cols = collidebox[xcol, ycol + 1];
                    for (int k = 0; k < cols.Count; k++)
                    {
                        if (Collide(mywizard, cols[k]))
                        {
                            
                            collide = true;
                            break;
                        }
                    }
                }

                if (!collide)
                    mywizard.pos = npos;
                mywizard.angle += m.Turn;
                if (mywizard.angle > Math.PI)
                    mywizard.angle = mywizard.angle - Math.PI - Math.PI;
                if (mywizard.angle < -Math.PI)
                    mywizard.angle = mywizard.angle + Math.PI + Math.PI;

                for (int k = 0; k < projs.Count; k++)
                {
                    projs[k].pos += projs[k].speed;
                    MyProjectile missle = projs[k];
                    bool next = true;
                    if (Vector.Distance(missle.startpos, missle.pos) > missle.mincast)
                    {
                        for (int n = 0; n < wizards.Length; n++)
                        {
                            if (Collide(missle, wizards[n]))
                            {
                                switch (missle.Type)
                                {
                                    case ProjectileType.MagicMissile:
                                        wizards[n].Life -= game.MagicMissileDirectDamage;
                                        
                                        break;
                                }

                                next = false;
                                projs.RemoveAt(k);
                                k--;
                                break;
                            }
                        }

                        if (!next)
                            continue;

                        for (int n = 0; n < buildings.Length; n++)
                        {
                            if (Collide(missle, buildings[n]))
                            {
                                switch (missle.Type)
                                {
                                    case ProjectileType.MagicMissile:
                                        buildings[n].Life -= game.MagicMissileDirectDamage;
                                        
                                        break;
                                }

                                next = false;
                                projs.RemoveAt(k);
                                k--;
                                break;
                            }
                        }


                        if (!next)
                            continue;

                        for (int n = 0; n < minions.Length; n++)
                        {
                            if (Collide(missle, minions[n]))
                            {
                                switch (missle.Type)
                                {
                                    case ProjectileType.MagicMissile:
                                        minions[n].Life -= game.MagicMissileDirectDamage;
                                        
                                        break;
                                }

                                next = false;
                                projs.RemoveAt(k);
                                k--;
                                break;
                            }
                        }

                        if (!next)
                            continue;
                    }
                    if (Vector.Distance(missle.startpos, missle.pos) > mywizard.CastRange)
                    {
                        projs.RemoveAt(k);
                        k--;
                    }
                }
            }

            for (int n = 0; n < buildings.Length; n++)
            {
                MyBuilding b = buildings[n];
                if (b.Type == BuildingType.GuardianTower)
                {
                    if (b.faction == enemyFaction)
                    {
                        //���� ������ ������ ��������� ����
                        if (b.RemainingActionCooldownTicks == 0)
                        {
                            if (Vector.Distance(b.pos, mywizard.pos) <= b.AttackRange)
                            {
                                mywizard.Life -= b.Damage;
                            }
                            b.RemainingActionCooldownTicks = b.CooldownTicks;
                        }
                        else
                        {
                            b.RemainingActionCooldownTicks--;
                            if (b.RemainingActionCooldownTicks < 0)
                                b.RemainingActionCooldownTicks = 0;
                        }
                    }
                }
            }

            

            mywizard.RemainingActionCooldownTicks -= steps;
            if (mywizard.RemainingActionCooldownTicks < 0)
                mywizard.RemainingActionCooldownTicks = 0;
            for (int k = 0; k < mywizard.RemainingCooldownTicksByAction.Length; k++)
            {
                mywizard.RemainingCooldownTicksByAction[k] -= steps;
                if (mywizard.RemainingCooldownTicksByAction[k] < 0)
                    mywizard.RemainingCooldownTicksByAction[k] = 0;
            }
        }

        public bool TestCollide(MyCircularUnit unit)
        {
            Vector npos = unit.pos;
            bool collide = false;
            int xcol = (int)(npos.x / cstep);
            int ycol = (int)(npos.y / cstep);
            if (npos.x <= unit.radius)
            {
                collide = true;
            }
            else if (npos.x >= w.Width - unit.radius)
            {
                collide = true;
            }
            if (npos.y <= unit.radius)
            {
                collide = true;
            }
            else if (npos.y >= w.Height - unit.radius)
            {
                collide = true;
            }
            if (collide)
                return true;

            List<MyCircularUnit> cols;

            cols = collidebox[xcol, ycol];
            for (int k = 0; k < cols.Count; k++)
            {
                if (Collide(unit, cols[k]))
                {
                    return true;
                }
            }
            

            if (xcol > 0)
            {
                cols = collidebox[xcol - 1, ycol];
                for (int k = 0; k < cols.Count; k++)
                {
                    if (Collide(unit, cols[k]))
                    {
                        return true;
                    }
                }

                if(ycol > 0)
                {
                    cols = collidebox[xcol - 1, ycol - 1];
                    for (int k = 0; k < cols.Count; k++)
                    {
                        if (Collide(unit, cols[k]))
                        {
                            return true;
                        }
                    }
                }
                if(ycol < cwidth - 1)
                {
                    cols = collidebox[xcol - 1, ycol + 1];
                    for (int k = 0; k < cols.Count; k++)
                    {
                        if (Collide(unit, cols[k]))
                        {
                            return true;
                        }
                    }
                }
            }

            if (xcol < cwidth - 1)
            {
                cols = collidebox[xcol + 1, ycol];
                for (int k = 0; k < cols.Count; k++)
                {
                    if (Collide(unit, cols[k]))
                    {
                        return true;
                    }
                }

                if (ycol > 0)
                {
                    cols = collidebox[xcol + 1, ycol - 1];
                    for (int k = 0; k < cols.Count; k++)
                    {
                        if (Collide(unit, cols[k]))
                        {
                            return true;
                        }
                    }
                }
                if (ycol < cheight - 1)
                {
                    cols = collidebox[xcol + 1, ycol + 1];
                    for (int k = 0; k < cols.Count; k++)
                    {
                        if (Collide(unit, cols[k]))
                        {
                            return true;
                        }
                    }
                }
            }

            if (ycol > 0)
            {
                cols = collidebox[xcol, ycol - 1];
                for (int k = 0; k < cols.Count; k++)
                {
                    if (Collide(unit, cols[k]))
                    {
                        return true;
                    }
                }
            }

            if (ycol < cheight - 1)
            {
                cols = collidebox[xcol, ycol + 1];
                for (int k = 0; k < cols.Count; k++)
                {
                    if (Collide(unit, cols[k]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        bool Collide(MyCircularUnit u1, MyCircularUnit u2)
        {
            if (Vector.Distance(u1.pos, u2.pos) <= (u1.radius + u2.radius))
                return true;
            else return false;
        }
    }


    class Vector
    {
        public double x;
        public double y;

        public Vector()
        {
            x = 0;
            y = 0;
        }

        public static Vector Position(Unit unit)
        {
            
            return new Vector(unit.X, unit.Y);
        }

        public static Vector Speed(Unit unit)
        {
            return new Vector(unit.SpeedX, unit.SpeedY);
        }

        public static Vector Forward(Unit unit)
        {
            return new Vector(Math.Cos(unit.Angle), Math.Sin(unit.Angle));
        }

        public static Vector Forward(MyUnit unit)
        {
            return new Vector(Math.Cos(unit.angle), Math.Sin(unit.angle));
        }

        public static Vector Forward(double angle)
        {
            return new Vector(Math.Cos(angle), Math.Sin(angle));
        }

        public Vector(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        public static Vector Zero
        {
            get
            {
                return new Vector();
            }
        }

        public static Vector operator-(Vector v1, Vector v2)
        {
            return new Vector(v1.x - v2.x, v1.y - v2.y);
        }

        public Vector Minus(Vector v)
        {
            x -= v.x;
            y -= v.y;
            return this;
        }

        public Vector Plus(Vector v)
        {
            x += v.x;
            y += v.y;
            return this;
        }

        public Vector Multiply(double val)
        {
            x *= val;
            y *= val;
            return this;
        }

        public Vector Subdivide(double val)
        {
            x /= val;
            y /= val;
            return this;
        }

        public static Vector operator +(Vector v1, Vector v2)
        {
            return new Vector(v1.x + v2.x, v1.y + v2.y);
        }

        public static Vector operator *(Vector v1, double v2)
        {
            return new Vector(v1.x * v2, v1.y * v2);
        }

        public static Vector operator /(Vector v1, double v2)
        {
            return new Vector(v1.x / v2, v1.y / v2);
        }

        public static double operator*(Vector v1, Vector v2)
        {
            return v1.x * v2.x + v1.y * v2.y;
        }

        public static double CosTo(MyUnit unitFrom, MyUnit unitTo)
        {
            Vector v = unitTo.pos - unitFrom.pos;
            v.Normalize();
            return v * Vector.Forward(unitFrom);
        }
        
        public double Abs
        {
            get
            {
                return Math.Sqrt(x * x + y * y);
            }
        }

        public double Abs2
        {
            get
            {
                return x * x + y * y;
            }
        }

        public Vector Normalize()
        {
            double a = Abs;
            if (a != 0)
            {
                x /= a;
                y /= a;
            }

            return this;
        }

        public static double Distance(Vector v1, Vector v2)
        {
            double dx = v1.x - v2.x;
            double dy = v1.y - v2.y;

            return Math.Sqrt(dx * dx + dy * dy);
        }

        public static double Distance(Unit v1, Unit v2)
        {
            double dx = v1.X - v2.X;
            double dy = v1.Y - v2.Y;

            return Math.Sqrt(dx * dx + dy * dy);
        }

        public static double Distance(Unit v1, Vector v2)
        {
            double dx = v1.X - v2.x;
            double dy = v1.Y - v2.y;

            return Math.Sqrt(dx * dx + dy * dy);
        }

        public static double Distance(MyUnit v1, MyUnit v2)
        {
            double dx = v1.pos.x - v2.pos.x;
            double dy = v1.pos.y - v2.pos.y;

            return Math.Sqrt(dx * dx + dy * dy);
        }

        public static double Distance(Vector v1, Unit v2)
        {
            double dx = v1.x - v2.X;
            double dy = v1.y - v2.Y;

            return Math.Sqrt(dx * dx + dy * dy);
        }

        public static double Distance2(Vector v1, Vector v2)
        {
            double dx = v1.x - v2.x;
            double dy = v1.y - v2.y;

            return dx * dx + dy * dy;
        }

        

        public override string ToString()
        {
            return "{" + x.ToString() + " " + y.ToString() + "}";
        }
    }

    
}