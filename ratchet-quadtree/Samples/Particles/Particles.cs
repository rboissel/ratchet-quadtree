using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Particles
{
    class Particles
    {
        public class Particle
        {
            internal float _x;
            internal float _y;
            internal float _fx;
            internal float _fy;
            public Particle(float x, float y)
            {
                _x = x;
                _y = y;
            }
        }

        public class AgregatedParticle
        {
            internal float _x;
            internal float _y;

            internal int _weigth;


            public AgregatedParticle(float x, float y)
            {
                _x = x;
                _y = y;
                _weigth = 1;
            }
        }

        Particle[] _Swarm;
        
        public Ratchet.Collections.Quadtree<AgregatedParticle> CreateGrid()
        {
            Ratchet.Collections.Quadtree<AgregatedParticle> grid = new Ratchet.Collections.Quadtree<AgregatedParticle>();
            for (int n = 0; n < _Swarm.Length; n++)
            {
                var node = grid.GetNode((long)_Swarm[n]._x, (long)_Swarm[n]._y, 1, 1);
                AgregatedParticle agregatedParticles = node.Element;
                if (agregatedParticles == null)
                {
                    agregatedParticles = new AgregatedParticle(node.X + (float)node.Size / 2.0f, node.Y + (float)node.Size / 2.0f);
                    node.Element = agregatedParticles;
                }
                else
                {
                    agregatedParticles._weigth++;
                }
            }
            return grid;
        }

        public void Refresh()
        {
            ulong particles_evaluated = 0;

            Ratchet.Collections.Quadtree<AgregatedParticle> particles = CreateGrid();
            Parallel.ForEach<Particle>(_Swarm, (Particle particle) =>
            {

                    float x = particle._x;
                    float y = particle._y;

                    float fx = 0;
                    float fy = 0;

                    foreach (Ratchet.Collections.Quadtree<AgregatedParticle>.Node neighboor in particles.GetNode((int)x - 15, (int)y - 15, 32, 32))
                    {
                        if (neighboor.Element != null)
                        {
                            particles_evaluated++;

                            float nx = neighboor.Element._x;
                            float ny = neighboor.Element._y;
                            int weight = neighboor.Element._weigth;
                            float dx = (x - nx) * (x - nx);
                            float dy = (y - ny) * (y - ny);

                            // Dirty hack to not count self interaction
                            if ((dx < float.Epsilon && dy < float.Epsilon)) { continue; }

                            float d2 = (x - nx) * (x - nx) + (y - ny) * (y - ny);
                            float d = (float)System.Math.Sqrt((double)d2);

                            // Only look at particles in a small radius
                            if (d > 30f) { continue; }

                            float dirx = (float)(nx - x) / d;
                            float diry = (float)(ny - y) / d;

                            // Again a hack to avoid some instabilities
                            if (d2 < 1.0f) { d2 = 1.0f; }

                            float f = (0.02f * (float)weight) / d2;

                            fx += dirx * f;
                            fy += diry * f;

                        }
                    }

                particle._fx += fx;
                particle._fy += fy;

                particle._x += particle._fx;
                particle._y += particle._fy;
            });
        }

        public unsafe void Draw(System.Drawing.Bitmap bitmap, bool DrawTree)
        {
            Refresh();

            int width = bitmap.Width;
            int height = bitmap.Height;

            var lockedBitmap = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            byte* ptr = (byte*)lockedBitmap.Scan0.ToPointer();

            for (int y = 0; y < width; y++)
            {
                for (int x = 0; x < height; x++)
                {
                    int offset = (x + y * width) * 4;
                    ptr[offset] = 0;
                    ptr[offset + 1] = 0;
                    ptr[offset + 2] = 0;
                    ptr[offset + 3] = 255;

                }
            }

            for (int n = 0; n < _Swarm.Length; n++)
            {
                if (_Swarm[n]._x < 0 || _Swarm[n]._x >= width || _Swarm[n]._y < 0 || _Swarm[n]._y >= height) { continue; }

                int offset = ((int)_Swarm[n]._x + (int)_Swarm[n]._y * width) * 4;

                byte* b = &ptr[offset];
                byte* g = &ptr[offset + 1];
                byte* r = &ptr[offset + 2];


                if (*r < (256 - 64))
                {
                    *r = (byte)(*r + 64);
                    *g = (byte)(*g + 32);
                    *b = (byte)(*b + 16);

                }
                else
                {
                    *r = 255;
                    if (*g < (256 - 32))
                    {
                        *g = (byte)(*g + 32);
                        *b = (byte)(*b + 16);
                    }
                    else if (*b < (256 - 32))
                    {
                        *g = 255;
                        *b = (byte)(*b + 16);
                    }
                    else
                    {
                        *b = 255;
                    }
                }

            }
            
            bitmap.UnlockBits(lockedBitmap);

        }

        public Particles(int GridSize, int ParticleCount)
        {
            _Swarm = new Particle[ParticleCount];
            Random rnd = new Random();
            rnd = new Random(42);

            for (int n = 0; n < ParticleCount; n++)
            {
                float distance = (float)rnd.NextDouble();
                distance = (distance * distance + 0.4f) * (GridSize / 6);

                float vx = (float)rnd.NextDouble() - 0.5f;
                float vy = (float)rnd.NextDouble() - 0.5f;


                float norm = (float)System.Math.Sqrt((vx * vx) + (vy * vy));
                vx /= norm;
                vy /= norm;


                _Swarm[n] = new Particle(distance * vx + GridSize / 2, distance * vy + GridSize / 2);
            }


        }
    }
}
