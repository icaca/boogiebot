/*
 *  Part of PPather
 *  Copyright Pontus Borg 2008
 * 
 */
using System;
using System.Collections.Generic;
using System.Text;
using Wmo;

namespace WowTriangles
{
    public class MPQTriangleSupplier : TriangleSupplier
    {
        string continentFile;

        StormDll.ArchiveSet archive;

        //TriangleSet global_triangles = new TriangleSet();
       
        WDT wdt;
        WDTFile wdtf;
        ModelManager modelmanager;
        WMOManager wmomanager;


        Dictionary<String, int> zoneToMapId = new Dictionary<string,int>();
        Dictionary<int, String> mapIdToFile = new Dictionary<int,string>();

        Dictionary<int, String> areaIdToName = new Dictionary<int, string>();        

        public override void Close()
        {
            archive.Close();
            wdt = null;
            wdtf = null;
            modelmanager = null;
            wmomanager = null;
            zoneToMapId = null;
            mapIdToFile = null;
            areaIdToName = null;
            archive = null;
            base.Close(); 
        }

        public MPQTriangleSupplier()
        {
             string[] archiveNames = {
                "patch-2.MPQ", 
                "patch.MPQ", 
                "enUS\\patch-enUS-2.MPQ",                
                "enUS\\patch-enUS.MPQ",
                "enGB\\patch-enGB-2.MPQ",
                "enGB\\patch-enGB.MPQ",
                "common.MPQ", 
                "expansion.MPQ", 
		        "enUS\\locale-enUS.MPQ", "enUS\\expansion-locale-enUS.MPQ", 
		        "enGB\\locale-enGB.MPQ", "enGB\\expansion-locale-enGB.MPQ"};
            
            //StormDll.ArchiveSet archive = null;
         
            archive = new StormDll.ArchiveSet();
            string regGameDir = archive.SetGameDirFromReg(); 
            //string gameDir = "F:\\World of Warcraft\\Data\\"; 
            //archive.SetGameDir(gameDir);

            Console.WriteLine("Game dir is " + regGameDir);
            archive.AddArchives(archiveNames);
            modelmanager = new ModelManager(archive, 80);
            wmomanager = new WMOManager(archive, modelmanager, 30);
           

            archive.ExtractFile("DBFilesClient\\AreaTable.dbc", "AreaTable.dbc");
            DBC areas = new DBC();
            DBCFile af = new DBCFile("AreaTable.dbc", areas);
            for (int i = 0; i < areas.recordCount; i++)
            {
                int AreaID = (int)areas.GetUint(i, 0);
                int WorldID = (int)areas.GetUint(i, 1);
                int Parent = (int)areas.GetUint(i, 2);
                string Name = areas.GetString(i, 11);

                areaIdToName.Add(AreaID, Name);
 
                
                if (WorldID != 0 && WorldID != 1 && WorldID != 530)
                {
                    ////   Console.WriteLine(String.Format("{0,4} {1,3} {2,3} {3}", AreaID, WorldID, Parent, Name));
                }
                //0 	 uint 	 AreaID
                //1 	uint 	Continent (refers to a WorldID)
                //2 	uint 	Region (refers to an AreaID)
            }

            for (int i = 0; i < areas.recordCount; i++)
            {
                int AreaID = (int)areas.GetUint(i, 0);
                int WorldID = (int)areas.GetUint(i, 1);
                int Parent = (int)areas.GetUint(i, 2);
                string Name = areas.GetString(i, 11);

                string TotalName = "";
                //areaIdToName.Add(AreaID, Name);
                //areaIdParent.Add(AreaID, Parent);
                string ParentName = "";
                if (!areaIdToName.TryGetValue(Parent, out ParentName))
                {
                    TotalName = ":" + Name; 
                }
                else
                    TotalName = Name + ":"+ParentName;
                try
                {
                    zoneToMapId.Add(TotalName, WorldID);
                    Console.WriteLine(TotalName + " => " + WorldID);
                }catch
                {
                    int id;
                    zoneToMapId.TryGetValue(TotalName, out id);
                    ////  Console.WriteLine("Duplicate: " + TotalName + " " + WorldID +" " + id);
                }
                //0 	 uint 	 AreaID
                //1 	uint 	Continent (refers to a WorldID)
                //2 	uint 	Region (refers to an AreaID)
            }
        }

        public string SetZone(string zone)
        {
            int continentID;

            if (!zoneToMapId.TryGetValue(zone, out continentID))
            {
                int colon = zone.IndexOf(":"); 
                if(colon == -1)
                    return null;
                zone = zone.Substring(colon);
                if (!zoneToMapId.TryGetValue(zone, out continentID))
                {
                    return null; 
                }
            }

            archive.ExtractFile("DBFilesClient\\Map.dbc", "Map.dbc");
            DBC maps = new DBC();
            DBCFile mf = new DBCFile("Map.dbc", maps);

            
            for (int i = 0; i < maps.recordCount; i++)
            {
                int mapID = maps.GetInt(i, 0);
               // Console.WriteLine("   ID:" + maps.GetInt(i, 0));                
               // Console.WriteLine(" File: " + maps.GetString(i, 1));
               // Console.WriteLine(" Name: " + maps.GetString(i, 4)); // the file!!!

                if (mapID == continentID) // file == continentFile)
                {
                    //  Console.WriteLine(String.Format("{0,4} {1}", mapID, maps.GetString(i, 1)));
                    string file = maps.GetString(i, 1);
                    continentFile = file;
                    
                    

                    wdt = new WDT();

                    wdtf = new WDTFile(archive, continentFile, wdt, wmomanager, modelmanager);
                    if (!wdtf.loaded)
                        wdt = null; // bad
                    else
                    {

                       // Console.WriteLine("  global Objects " + wdt.gwmois.Count + " Models " + wdt.gwmois.Count);
                        //global_triangles.color = new float[3] { 0.8f, 0.8f, 1.0f };
                    }
                    return continentFile; 
                }
            }
            if (wdt == null)
            {
                return "Failed to open file files for continent ID" + continentID; 
            }
            return null;
        }

         
       
       
        private void GetChunkData(TriangleCollection triangles, int chunk_x, int chunk_y, SparseMatrix3D<WMO> instances)
        {
            if (chunk_x < 0) return;
            if (chunk_y < 0) return;
            if (chunk_x > 63) return;
            if (chunk_y > 63) return; 
            /*
            if (chunkCache[chunk_x, chunk_y] != null)
            {
                chunkCache[chunk_x, chunk_y].LRU = NOW++; 
                return chunkCache[chunk_x, chunk_y];
            }
            else
            {
                EvictFromChunkCache(); 
            }


            float max_x = ChunkReader.ZEROPOINT - (float)(chunk_y) * ChunkReader.TILESIZE;
            float max_y = ChunkReader.ZEROPOINT - (float)(chunk_x) * ChunkReader.TILESIZE;
            float min_x = max_x - ChunkReader.TILESIZE;
            float min_y = max_y - ChunkReader.TILESIZE; 


            ChunkData myChunk = new ChunkData();
            myChunk.LRU = NOW++; 
            chunkCache[chunk_x, chunk_y] = myChunk;
            chunkCacheItems++;

            TriangleCollection triangles = new TriangleCollection();
            myChunk.triangles = triangles;
            triangles.SetLimits(min_x, min_y, -1E30f, max_x, max_y, 1E30f); 
            */
          
            //Console.WriteLine("x " + max_x + " y " + max_y);

           // Console.Write(String.Format(" Tile {0,2} {1,2}", chunk_x, chunk_y));
          //  Console.Write(" load"); 
            if (triangles == null) return; 

            if (wdtf == null) return;
            if (wdt == null) return;
            wdtf.LoadMapTile(chunk_x, chunk_y);
            

            MapTile t = wdt.maptiles[chunk_x, chunk_y];
            if (t != null)
            {
                //Console.Write(" render"); 
                // Map tiles                
                for (int ci = 0; ci < 16; ci++)
                {
                    for (int cj = 0; cj < 16; cj++)
                    {
                        MapChunk c = t.chunks[ci, cj];
                        if(c != null)
                            AddTriangles(triangles, c);
                    }
                }
  
                // World objects
                
                foreach (WMOInstance wi in t.wmois)
                {
                    if (wi != null && wi.wmo != null)
                    {
                        String fn = wi.wmo.fileName;
                        int last = fn.LastIndexOf('\\');
                        fn = fn.Substring(last + 1);
                        // Console.WriteLine("    wmo: " + fn + " at " + wi.pos);
                        if(fn != null)
                        {

                            WMO old = instances.Get((int)wi.pos.x, (int)wi.pos.y, (int)wi.pos.z);
                            if (old == wi.wmo)
                            {
                                //Console.WriteLine("Already got " + fn);
                            }
                            else
                            {
                                instances.Set((int)wi.pos.x, (int)wi.pos.y, (int)wi.pos.z, wi.wmo);
                                AddTriangles(triangles, wi);

                            }
                         }
                    }
                }
                
                foreach (ModelInstance mi in t.modelis)
                {
                    if (mi != null && mi.model != null)
                    {
                        String fn = mi.model.fileName;
                        int last = fn.LastIndexOf('\\');
                        // fn = fn.Substring(last + 1);
                        //Console.WriteLine("    wmi: " + fn + " at " + mi.pos);
                        AddTriangles(triangles, mi);

                        //Console.WriteLine("    model: " + fn);
                    }
                }
                
                

                Console.WriteLine("wee"); 
                /*Console.WriteLine(
                    String.Format(" Triangles - Map: {0,6} Objects: {1,6} Models: {2,6}",
                                  map_triangles.GetNumberOfTriangles(),
                                  obj_triangles.GetNumberOfTriangles(),
                                  model_triangles.GetNumberOfTriangles()));
                */

            }
            Console.WriteLine(" done"); 
            wdt.maptiles[chunk_x, chunk_y] = null; // clear it atain
            //myChunk.triangles.ClearVertexMatrix(); // not needed anymore
            //return myChunk;
        }

        private void GetChunkCoord(float x, float y, out int chunk_x, out int chunk_y)
        {
            // yeah, this is ugly. But safe
            for (chunk_x = 0; chunk_x < 64; chunk_x++)
            {
                float max_y = ChunkReader.ZEROPOINT - (float)(chunk_x) * ChunkReader.TILESIZE;
                float min_y = max_y - ChunkReader.TILESIZE;
                if (y >= min_y- 0.1f && y < max_y + 0.1f) break; 
            }
            for (chunk_y = 0; chunk_y < 64; chunk_y++)
            {
                float max_x = ChunkReader.ZEROPOINT - (float)(chunk_y) * ChunkReader.TILESIZE;
                float min_x = max_x - ChunkReader.TILESIZE;
                if (x >= min_x - 0.1f && x < max_x + 0.1f) break; 
            }
            if (chunk_y == 64 || chunk_x == 64)
            {
                Console.WriteLine(x + " " + y + " is at " + chunk_x + " " + chunk_y);
                //GetChunkCoord(x, y, out chunk_x, out chunk_y); 
            }
        }

        public override void GetTriangles(TriangleCollection to, float min_x, float min_y, float max_x, float max_y)
        {
            Console.WriteLine("TotalMemory " + System.GC.GetTotalMemory(false)/(1024*1024) + " MB");
            foreach (WMOInstance wi in wdt.gwmois)
            {
                AddTriangles(to, wi);
            }
            SparseMatrix3D<WMO> instances = new SparseMatrix3D<WMO>();
            for (float x = min_x; x < max_x ; x += ChunkReader.TILESIZE)
            {
                for (float y = min_y; y < max_y ; y += ChunkReader.TILESIZE)
                {
                    int chunk_x, chunk_y;
                    GetChunkCoord(x, y, out chunk_x, out chunk_y); 
                    /*ChunkData d = */GetChunkData(to, chunk_x, chunk_y, instances);
                    
                    //to.AddAllTrianglesFrom(d.triangles); 
                }
            } 
            

        }

        void AddTriangles(TriangleCollection s, MapChunk c)
        {
            int[,] vertices = new int[9, 9];
            int[,] verticesMid = new int[8, 8];
            
            for (int row = 0; row < 9; row++)
                for (int col = 0; col < 9; col++)
                {
                    float x, y, z;
                    ChunkGetCoordForPoint(c, row, col, out x, out y, out z);
                    int index = s.AddVertex(x, y, z);
                    vertices[row, col] = index;
                }

            for (int row = 0; row < 8; row++)
                for (int col = 0; col < 8; col++)
                {
                    float x, y, z;
                    ChunkGetCoordForMiddlePoint(c, row, col, out x, out y, out z);
                    int index = s.AddVertex(x, y, z);
                    verticesMid[row, col] = index;
                }
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    if (!c.isHole(col, row))
                    {
                        int v0 = vertices[row, col];
                        int v1 = vertices[row + 1, col];
                        int v2 = vertices[row + 1, col + 1];
                        int v3 = vertices[row, col + 1];
                        int vMid = verticesMid[row, col];

                        s.AddTriangle(v0, v1, vMid);
                        s.AddTriangle(v1, v2, vMid);
                        s.AddTriangle(v2, v3, vMid);
                        s.AddTriangle(v3, v0, vMid);
                        
                    }
                }
            }

            if (c.haswater)
            { 
                // paint the water
                for (int row = 0; row < 9; row++)
                    for (int col = 0; col < 9; col++)
                    {
                        float x, y, z;
                        ChunkGetCoordForPoint(c, row, col, out x, out y, out z);
                        float height = c.water_height[row, col]-1.5f;
                        int index = s.AddVertex(x, y,  height);
                        vertices[row, col] = index;
                    }
                for (int row = 0; row < 8; row++)
                {
                    for (int col = 0; col < 8; col++)
                    {
                        if (c.water_flags[row, col] != 0xf)
                        { 
                            int v0 = vertices[row, col];
                            int v1 = vertices[row + 1, col];
                            int v2 = vertices[row + 1, col + 1];
                            int v3 = vertices[row, col + 1];

                            s.AddTriangle(v0, v1, v3, ChunkedTriangleCollection.TriangleFlagDeepWater);
                            s.AddTriangle(v1, v2, v3, ChunkedTriangleCollection.TriangleFlagDeepWater); 
                        }
                    }
                }
            }

        }


        void AddTriangles(TriangleCollection s, WMOInstance wi)
        {
            float dx = wi.pos.x;
            float dy = wi.pos.y;
            float dz = wi.pos.z;

            float dir_x = wi.dir.x;
            float dir_y = wi.dir.y;
            float dir_z = wi.dir.z;

            WMO wmo = wi.wmo;
            
            foreach (WMOGroup g in wmo.groups)
            {
                int[] vertices = new int[g.nVertices];

                for (int i = 0; i < g.nVertices; i++)
                {
                    int off = i * 3;

                    float x = g.vertices[off];
                    float y = g.vertices[off + 2];
                    float z = g.vertices[off + 1];

                    rotate(x, z, (dir_y - 90), out x, out z);
                    rotate(x, y, dir_z, out x, out y);
                    rotate(y, z, -dir_x, out y, out z);

                    float xx = x + dx;
                    float yy = y + dy;
                    float zz = -z + dz;

                    float finalx = ChunkReader.ZEROPOINT - zz;
                    float finaly = ChunkReader.ZEROPOINT - xx;
                    float finalz = yy;

                    vertices[i] = s.AddVertex(finalx, finaly, finalz);
                }
                // Console.WriteLine("nTriangles: " + g.nTriangles); 
                for (int i = 0; i < g.nTriangles; i++)
                {
                    //if ((g.materials[i] & 0x1000) != 0)
                    {
                        int off = i * 3;
                        int i0 = vertices[g.triangles[off]];
                        int i1 = vertices[g.triangles[off + 1]];
                        int i2 = vertices[g.triangles[off + 2]];

                        int t = s.AddTriangle(i0, i1, i2, ChunkedTriangleCollection.TriangleFlagObject);
                        //if(t != -1) s.SetTriangleExtra(t, g.materials[0], 0, 0); 
                    }
                }
            }
             
            int doodadset = wi.doodadset;


            if (doodadset < wmo.nDoodadSets)
            {
                uint firstDoodad = wmo.doodads[doodadset].firstInstance;
                uint nDoodads = wmo.doodads[doodadset].nInstances;

                for (uint i = 0; i < nDoodads; i++)
                {
                    uint d = firstDoodad + i;
                    ModelInstance mi = wmo.doodadInstances[d];
                    if (mi != null)
                    {
                        //Console.WriteLine("I got model " + mi.model.fileName + " at " + mi.pos);
                        //AddTrianglesGroupDoodads(s, mi, wi.dir, wi.pos, 0.0f);
                    }
                }
            }
        
        }


        void AddTrianglesGroupDoodads(TriangleCollection s, ModelInstance mi, Vec3D world_dir, Vec3D world_off, float rot)
        {
            float dx = mi.pos.x;
            float dy = mi.pos.y;
            float dz = mi.pos.z;

            rotate(dx, dz, rot +90f, out dx, out dz);


            dx += world_off.x;
            dy += world_off.y;
            dz += world_off.z;


            Quaternion q;
            q.x = -mi.dir.z; q.y = mi.dir.x; q.z = mi.dir.y; q.w = mi.w;
            Matrix4 rotMatrix = new Matrix4();
            rotMatrix.makeQuaternionRotate(q);


            Model m = mi.model;

            if (m.boundingTriangles == null)
            {
                /*
                // /cry no bouding info, revert to normal vertices
                
                ModelView mv = m.view[0]; // View number 1 ?!?!
                int[] vertices = new int[m.vertices.Length / 3];
                for (uint i = 0; i < m.vertices.Length / 3; i++)
                {
                    float x = m.vertices[i * 3];
                    float y = m.vertices[i * 3 + 2];
                    float z = m.vertices[i * 3 + 1];
                    x *= mi.sc;
                    y *= mi.sc;
                    z *= -mi.sc;

                    Vector pos; pos.x = x; pos.y = y; pos.z = z;
                    Vector new_pos = rotMatrix.mutiply(pos);
                    x = pos.x; y = pos.y; z = pos.z;

                    rotate(x, z, (world_dir.y - 90), out x, out z);
                    rotate(y, z, -world_dir.x, out y, out z);
                    rotate(x, y, world_dir.z, out x, out y);

                    float xx = x + dx;
                    float yy = y + dy;
                    float zz = -z + dz;

                    float finalx = ChunkReader.ZEROPOINT - zz;
                    float finaly = ChunkReader.ZEROPOINT - xx;
                    float finalz = yy;

                    vertices[i] = s.AddVertex(finalx, finaly, finalz);
                }


                for (int i = 0; i < mv.triangleList.Length / 3; i++)
                {
                    int off = i * 3;
                    UInt16 vi0 = mv.triangleList[off];
                    UInt16 vi1 = mv.triangleList[off + 1];
                    UInt16 vi2 = mv.triangleList[off + 2];

                    int ind0 = mv.indexList[vi0];
                    int ind1 = mv.indexList[vi1];
                    int ind2 = mv.indexList[vi2];

                    int v0 = vertices[ind0];
                    int v1 = vertices[ind1];
                    int v2 = vertices[ind2];
                    s.AddTriangle(v0, v1, v2, ChunkedTriangleCollection.TriangleFlagModel);
                }
                */
            }
            else
            {
                
                // We got boiuding stuff, that is better
                int nBoundingVertices = m.boundingVertices.Length / 3;
                int[] vertices = new int[nBoundingVertices];
                
                for (uint i = 0; i < nBoundingVertices; i++)
                {
                    uint off = i * 3;
                    float x = m.boundingVertices[off];
                    float y = m.boundingVertices[off + 2];
                    float z = m.boundingVertices[off + 1];
                    x *= mi.sc;
                    y *= mi.sc;
                    z *= -mi.sc;

                    Vector pos; pos.x = x; pos.y = y; pos.z = z;
                    Vector new_pos = rotMatrix.mutiply(pos);
                    x = pos.x; y = pos.y; z = pos.z;

                    rotate(x, z, (world_dir.y - 90), out x, out z);
                    rotate(y, z, -world_dir.x, out y, out z);
                    rotate(x, y, world_dir.z, out x, out y);

                    float xx = x + dx;
                    float yy = y + dy;
                    float zz = -z + dz;

                    float finalx = ChunkReader.ZEROPOINT - zz;
                    float finaly = ChunkReader.ZEROPOINT - xx;
                    float finalz = yy;
                    vertices[i] = s.AddVertex(finalx, finaly, finalz);
                }
                

                int nBoundingTriangles = m.boundingTriangles.Length / 3;
                for (uint i = 0; i < nBoundingTriangles; i++)
                {
                    uint off = i * 3;
                    int v0 = vertices[m.boundingTriangles[off]];
                    int v1 = vertices[m.boundingTriangles[off + 1]];
                    int v2 = vertices[m.boundingTriangles[off + 2]];
                    s.AddTriangle(v0, v1, v2, ChunkedTriangleCollection.TriangleFlagModel);
                }
            }
        }

        void AddTriangles(TriangleCollection s, ModelInstance mi)
        {
            float dx = mi.pos.x; 
            float dy = mi.pos.y; 
            float dz = mi.pos.z;
    
            
            float dir_x = mi.dir.z;
            float dir_y = mi.dir.y;
            float dir_z = mi.dir.x;
    
       
            Model m = mi.model;

            if (m.boundingTriangles == null)
            {
                
                // /cry no bouding info, revert to normal vertives

                ModelView mv = m.view[0]; // View number 1 ?!?!
                int[] vertices = new int[m.vertices.Length / 3];
                for (uint i = 0; i < m.vertices.Length / 3; i++)
                {
                    float x = m.vertices[i * 3];
                    float y = m.vertices[i * 3 + 2];
                    float z = m.vertices[i * 3 + 1];
                    x *= mi.sc;
                    y *= mi.sc;
                    z *= mi.sc;

                    rotate(x, y, dir_z, out x, out y);
                    rotate(y, z, -dir_x, out y, out z);
                    rotate(x, z, (dir_y - 90), out x, out z);
                    
                    

                    float xx = x + dx;
                    float yy = y + dy;
                    float zz = -z + dz;

                    float finalx = ChunkReader.ZEROPOINT - zz;
                    float finaly = ChunkReader.ZEROPOINT - xx;
                    float finalz = yy;

                    vertices[i] = s.AddVertex(finalx, finaly, finalz);
                }


                for (int i = 0; i < mv.triangleList.Length / 3; i++)
                {
                    int off = i * 3;
                    UInt16 vi0 = mv.triangleList[off];
                    UInt16 vi1 = mv.triangleList[off + 1];
                    UInt16 vi2 = mv.triangleList[off + 2];

                    int ind0 = mv.indexList[vi0];
                    int ind1 = mv.indexList[vi1];
                    int ind2 = mv.indexList[vi2];

                    int v0 = vertices[ind0];
                    int v1 = vertices[ind1];
                    int v2 = vertices[ind2];
                    s.AddTriangle(v0, v1, v2, ChunkedTriangleCollection.TriangleFlagModel);
                }
                
            }
            else
            {
                // We got boiuding stuff, that is better
                int nBoundingVertices = m.boundingVertices.Length / 3;
                int[] vertices = new int[nBoundingVertices];
                for (uint i = 0; i < nBoundingVertices; i++)
                {
                    uint off = i * 3;
                    float x = m.boundingVertices[off];
                    float y = m.boundingVertices[off + 2];
                    float z = m.boundingVertices[off + 1];
                    x *= mi.sc;
                    y *= mi.sc;
                    z *= mi.sc;

                    rotate(x, y, dir_z, out x, out y);
                    rotate(y, z, -dir_x, out y, out z);
                    rotate(x, z, (dir_y - 90), out x, out z);
                    

                    float xx = x + dx;
                    float yy = y + dy;
                    float zz = -z + dz;

                    float finalx = ChunkReader.ZEROPOINT - zz;
                    float finaly = ChunkReader.ZEROPOINT - xx;
                    float finalz = yy;

                    vertices[i] = s.AddVertex(finalx, finaly, finalz);
                }


                int nBoundingTriangles = m.boundingTriangles.Length / 3;
                for (uint i = 0; i < nBoundingTriangles; i++)
                {
                    uint off = i * 3;
                    int v0 = vertices[m.boundingTriangles[off]];
                    int v1 = vertices[m.boundingTriangles[off + 1]];
                    int v2 = vertices[m.boundingTriangles[off + 2]];
                    s.AddTriangle(v0, v1, v2, ChunkedTriangleCollection.TriangleFlagModel);
                }
            }
        }

        static void ChunkGetCoordForPoint(MapChunk c, int row, int col,
                                          out float x, out float y, out float z)
        {
            int off = (row * 17 + col) * 3;
            x = ChunkReader.ZEROPOINT - c.vertices[off + 2];
            y = ChunkReader.ZEROPOINT - c.vertices[off];
            z = c.vertices[off + 1];
        }

        static void ChunkGetCoordForMiddlePoint(MapChunk c, int row, int col,
                                            out float x, out float y, out float z)
        {
            int off = (9 + (row * 17 + col)) * 3;
            x = ChunkReader.ZEROPOINT - c.vertices[off + 2];
            y = ChunkReader.ZEROPOINT - c.vertices[off];
            z = c.vertices[off + 1];
        }







        public static void rotate(float x, float y, float angle, out float nx, out float ny)
        {
            double rot = (angle) / 360.0 * Math.PI * 2;
            float c_y = (float)Math.Cos(rot);
            float s_y = (float)Math.Sin(rot);


            nx = c_y * x - s_y * y;
            ny = s_y * x + c_y * y;
        }



    }
}
