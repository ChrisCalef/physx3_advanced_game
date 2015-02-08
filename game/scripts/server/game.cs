//-----------------------------------------------------------------------------
// Copyright (c) 2012 GarageGames, LLC
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//-----------------------------------------------------------------------------

//-----------------------------------------------------------------------------
// What kind of "player" is spawned is either controlled directly by the
// SpawnSphere or it defaults back to the values set here. This also controls
// which SimGroups to attempt to select the spawn sphere's from by walking down
// the list of SpawnGroups till it finds a valid spawn object.
// These override the values set in core/scripts/server/spawn.cs
//-----------------------------------------------------------------------------

// Leave $Game::defaultPlayerClass and $Game::defaultPlayerDataBlock as empty strings ("")
// to spawn a the $Game::defaultCameraClass as the control object.
$Game::DefaultPlayerClass = "";
$Game::DefaultPlayerDataBlock = "";
$Game::DefaultPlayerSpawnGroups = "CameraSpawnPoints PlayerSpawnPoints PlayerDropPoints";

//-----------------------------------------------------------------------------
// What kind of "camera" is spawned is either controlled directly by the
// SpawnSphere or it defaults back to the values set here. This also controls
// which SimGroups to attempt to select the spawn sphere's from by walking down
// the list of SpawnGroups till it finds a valid spawn object.
// These override the values set in core/scripts/server/spawn.cs
//-----------------------------------------------------------------------------
$Game::DefaultCameraClass = "Camera";
$Game::DefaultCameraDataBlock = "Observer";
$Game::DefaultCameraSpawnGroups = "CameraSpawnPoints PlayerSpawnPoints PlayerDropPoints";

// Global movement speed that affects all Cameras
$Camera::MovementSpeed = 30;

//-----------------------------------------------------------------------------
// GameConnection manages the communication between the server's world and the
// client's simulation. These functions are responsible for maintaining the
// client's camera and player objects.
//-----------------------------------------------------------------------------

//-----------------------------------------------------------------------------
// This is the main entry point for spawning a control object for the client.
// The control object is the actual game object that the client is responsible
// for controlling in the client and server simulations. We also spawn a
// convenient camera object for use as an alternate control object. We do not
// have to spawn this camera object in order to function in the simulation.
//
// Called for each client after it's finished downloading the mission and is
// ready to start playing.
//-----------------------------------------------------------------------------
function GameConnection::onClientEnterGame(%this)
{
   // This function currently relies on some helper functions defined in
   // core/scripts/spawn.cs. For custom spawn behaviors one can either
   // override the properties on the SpawnSphere's or directly override the
   // functions themselves.

   // Find a spawn point for the camera
   %cameraSpawnPoint = pickCameraSpawnPoint($Game::DefaultCameraSpawnGroups);
   // Spawn a camera for this client using the found %spawnPoint
   %this.spawnCamera(%cameraSpawnPoint);

   // Find a spawn point for the player
   %playerSpawnPoint = pickPlayerSpawnPoint($Game::DefaultPlayerSpawnGroups);
   // Spawn a camera for this client using the found %spawnPoint
   %this.spawnPlayer(%playerSpawnPoint);
}

//-----------------------------------------------------------------------------
// Clean up the client's control objects
//-----------------------------------------------------------------------------
function GameConnection::onClientLeaveGame(%this)
{
   // Cleanup the camera
   if (isObject(%this.camera))
      %this.camera.delete();
   // Cleanup the player
   if (isObject(%this.player))
      %this.player.delete();
}

//-----------------------------------------------------------------------------
// Handle a player's death
//-----------------------------------------------------------------------------
function GameConnection::onDeath(%this, %sourceObject, %sourceClient, %damageType, %damLoc)
{
   // Clear out the name on the corpse
   if (isObject(%this.player))
   {
      if (%this.player.isMethod("setShapeName"))
         %this.player.setShapeName("");
   }

    // Switch the client over to the death cam
    if (isObject(%this.camera) && isObject(%this.player))
    {
        %this.camera.setMode("Corpse", %this.player);
        %this.setControlObject(%this.camera);
    }

    // Unhook the player object
    %this.player = 0;
}

//-----------------------------------------------------------------------------
//  Server, mission, and game management
//-----------------------------------------------------------------------------

//-----------------------------------------------------------------------------
// The server has started up so do some game start up
//-----------------------------------------------------------------------------
function onServerCreated()
{
   // Server::GameType is sent to the master server.
   // This variable should uniquely identify your game and/or mod.
   $Server::GameType = "Torque 3D";

   // Server::MissionType sent to the master server.  Clients can
   // filter servers based on mission type.
   $Server::MissionType = "pureLIGHT";

   // GameStartTime is the sim time the game started. Used to calculated
   // game elapsed time.
   $Game::StartTime = 0;

   // Create the server physics world.
   physicsInitWorld( "server" );
   
   // Load up any objects or datablocks saved to the editor managed scripts
   %datablockFiles = new ArrayObject();
   %datablockFiles.add( "art/ribbons/ribbonExec.cs" );   
   %datablockFiles.add( "art/particles/managedParticleData.cs" );
   %datablockFiles.add( "art/particles/managedParticleEmitterData.cs" );
   %datablockFiles.add( "art/decals/managedDecalData.cs" );
   %datablockFiles.add( "art/datablocks/managedDatablocks.cs" );
   %datablockFiles.add( "art/forest/managedItemData.cs" );
   %datablockFiles.add( "art/datablocks/datablockExec.cs" );   
   loadDatablockFiles( %datablockFiles, true );

   // Run the other gameplay scripts in this folder
   exec("./scriptExec.cs");

   // Keep track of when the game started
   $Game::StartTime = $Sim::Time;
}

//-----------------------------------------------------------------------------
// This function is called as part of a server shutdown
//-----------------------------------------------------------------------------
function onServerDestroyed()
{
   // Destroy the server physcis world
   physicsDestroyWorld( "server" );   
}

//-----------------------------------------------------------------------------
// Called by loadMission() once the mission is finished loading
//-----------------------------------------------------------------------------
function onMissionLoaded()
{
   // Start the server side physics simulation
   physicsStartSimulation( "server" );

   // Nothing special for now, just start up the game play
   startGame();
}

//-----------------------------------------------------------------------------
// Called by endMission(), right before the mission is destroyed
//-----------------------------------------------------------------------------
function onMissionEnded()
{
   // Stop the server physics simulation
   physicsStopSimulation( "server" );
   
   // Normally the game should be ended first before the next
   // mission is loaded, this is here in case loadMission has been
   // called directly.  The mission will be ended if the server
   // is destroyed, so we only need to cleanup here.
   $Game::Running = false;
}

//-----------------------------------------------------------------------------
// Called once the game has started
//-----------------------------------------------------------------------------
function startGame()
{
    if ($Game::Running)
    {
        error("startGame(): End the game first!");
        return;
    }

    $Game::Running = true;
}

//-----------------------------------------------------------------------------
// Called once the game has ended
//-----------------------------------------------------------------------------
function endGame()
{
   if (!$Game::Running)
   {
      error("endGame(): No game running!");
      return;
   }

   // Inform the client the game is over
   for( %clientIndex = 0; %clientIndex < ClientGroup.getCount(); %clientIndex++ )
   {
      %cl = ClientGroup.getObject( %clientIndex );
      commandToClient(%cl, 'GameEnd');
   }

   // Delete all the temporary mission objects
   resetMission();
   $Game::Running = false;
}





//-----------------------------------------------------------------------------
// Physics Experimentation
//-----------------------------------------------------------------------------
function makeM4(%start)
{
   pdd(1);
   $m4 = new PhysicsShape() {
      playAmbient = "1";
      dataBlock = "M4Physics";
      position = %start;
      rotation = "0 0 1 180";
      canSave = "1";
      canSaveDynamicFields = "1";
      areaImpulse = "0";
      damageRadius = "0";
      invulnerable = "0";
      minDamageAmount = "0";
      radiusDamage = "0";
      hasGravity = false;
      isDynamic = false;
   };
}

function looseBoxes(%start)
{
   if (VectorLen(%start)==0.0)
      %start = "0 0 9";
      
   %b = new PhysicsShape() {
      playAmbient = "0";
      dataBlock = "PSCube";
      position = VectorAdd(%start,"3 0 0");
      rotation = "1 0 0 0";
      canSave = "1";
      canSaveDynamicFields = "1";
      areaImpulse = "0";
      damageRadius = "0";
      invulnerable = "0";
      minDamageAmount = "0";
      radiusDamage = "0";
      hasGravity = true;
      isDynamic = true;
   };
   %b = new PhysicsShape() {
      playAmbient = "0";
      dataBlock = "PSCube";
      position = VectorAdd(%start,"3 2 0");
      rotation = "1 0 0 0";
      canSave = "1";
      canSaveDynamicFields = "1";
      areaImpulse = "0";
      damageRadius = "0";
      invulnerable = "0";
      minDamageAmount = "0";
      radiusDamage = "0";
      hasGravity = true;
      isDynamic = true;
   }; 
   %b = new PhysicsShape() {
      playAmbient = "0";
      dataBlock = "PSCube";
      position = VectorAdd(%start,"3 4 0");
      rotation = "1 0 0 0";
      canSave = "1";
      canSaveDynamicFields = "1";
      areaImpulse = "0";
      damageRadius = "0";
      invulnerable = "0";
      minDamageAmount = "0";
      radiusDamage = "0";
      hasGravity = true;
      isDynamic = true;
   }; 
   %b = new PhysicsShape() {
      playAmbient = "0";
      dataBlock = "PSCube";
      position = VectorAdd(%start,"3 6 0");
      rotation = "1 0 0 0";
      canSave = "1";
      canSaveDynamicFields = "1";
      areaImpulse = "0";
      damageRadius = "0";
      invulnerable = "0";
      minDamageAmount = "0";
      radiusDamage = "0";
      hasGravity = true;
      isDynamic = true;
   }; 
   %b = new PhysicsShape() {
      playAmbient = "0";
      dataBlock = "PSCube";
      position = VectorAdd(%start,"3 8 0");
      rotation = "1 0 0 0";
      canSave = "1";
      canSaveDynamicFields = "1";
      areaImpulse = "0";
      damageRadius = "0";
      invulnerable = "0";
      minDamageAmount = "0";
      radiusDamage = "0";
      hasGravity = true;
      isDynamic = true;
   }; 
   %b = new PhysicsShape() {
      playAmbient = "0";
      dataBlock = "PSCube";
      position = VectorAdd(%start,"3 10 0");
      rotation = "1 0 0 0";
      canSave = "1";
      canSaveDynamicFields = "1";
      areaImpulse = "0";
      damageRadius = "0";
      invulnerable = "0";
      minDamageAmount = "0";
      radiusDamage = "0";
      hasGravity = true;
      isDynamic = true;
   }; 
}

function makeBox(%start)
{
   if (VectorLen(%start)==0.0)
      %start = "0 0 2";
   %a = new PhysicsShape() {
      playAmbient = "0";
      dataBlock = "PSCube";
      position = %start;
      rotation = "1 0 0 0";
      canSave = "1";
      canSaveDynamicFields = "1";
      areaImpulse = "0";
      damageRadius = "0";
      invulnerable = "0";
      minDamageAmount = "0";
      radiusDamage = "0";
      hasGravity = false;
      isDynamic = false;
   };   
}
function makeBoxes(%start)
{
   if (VectorLen(%start)==0.0)
      %start = "0 0 3";
      
   //%jointID = 1;
   %a = new PhysicsShape() {
      playAmbient = "0";
      dataBlock = "PSCube";
      position = VectorAdd(%start,"0 0 0");
      rotation = "1 0 0 0";
      canSave = "1";
      canSaveDynamicFields = "1";
      areaImpulse = "0";
      damageRadius = "0";
      invulnerable = "0";
      minDamageAmount = "0";
      radiusDamage = "0";
      hasGravity = false;
      isDynamic = false;
   };   
   $sphericalB = new PhysicsShape() {
      playAmbient = "0";
      dataBlock = "PSCube";
      position = VectorAdd(%start,"0 0 2");
      rotation = "1 0 0 0";
      canSave = "1";
      canSaveDynamicFields = "1";
      areaImpulse = "0";
      damageRadius = "0";
      invulnerable = "0";
      minDamageAmount = "0";
      radiusDamage = "0";
      hasGravity = true;
      isDynamic = true;
   };
   %a.jointAttach($sphericalB,3);//4 = spherical 
   ///////////////////////////////////////////////
   %a = new PhysicsShape() {
      playAmbient = "0";
      dataBlock = "PSCube";
      position = VectorAdd(%start,"0 2 0");
      rotation = "1 0 0 0";
      canSave = "1";
      canSaveDynamicFields = "1";
      areaImpulse = "0";
      damageRadius = "0";
      invulnerable = "0";
      minDamageAmount = "0";
      radiusDamage = "0";
      hasGravity = false;
      isDynamic = false;
   };   
   $revoluteB = new PhysicsShape() {
      playAmbient = "0";
      dataBlock = "PSCube";
      position = VectorAdd(%start,"3 2 0");
      rotation = "1 0 0 0";
      canSave = "1";
      canSaveDynamicFields = "1";
      areaImpulse = "0";
      damageRadius = "0";
      invulnerable = "0";
      minDamageAmount = "0";
      radiusDamage = "0";
      hasGravity = true;
      isDynamic = true;
   };
   %a.jointAttach($revoluteB,5);//REVOLUTE 5
   ///////////////////////////////////////////////   
   %a = new PhysicsShape() {
      playAmbient = "0";
      dataBlock = "PSCube";
      position =  VectorAdd(%start,"0 4 0");
      rotation = "1 0 0 0";
      canSave = "1";
      canSaveDynamicFields = "1";
      areaImpulse = "0";
      damageRadius = "0";
      invulnerable = "0";
      minDamageAmount = "0";
      radiusDamage = "0";
      hasGravity = false;
      isDynamic = false;
   };   
   $prismaticB = new PhysicsShape() {
      playAmbient = "0";
      dataBlock = "PSCube";
      position = VectorAdd(%start,"3 4 1");
      rotation = "1 0 0 0";
      canSave = "1";
      canSaveDynamicFields = "1";
      areaImpulse = "0";
      damageRadius = "0";
      invulnerable = "0";
      minDamageAmount = "0";
      radiusDamage = "0";
      hasGravity = true;
      isDynamic = true;
   };
   %a.jointAttach($prismaticB,6);//PRISMATIC 6
   ///////////////////////////////////////////////   
   %a = new PhysicsShape() {
      playAmbient = "0";
      dataBlock = "PSCube";
      position = VectorAdd(%start,"0 6 0");
      rotation = "1 0 0 0";
      canSave = "1";
      canSaveDynamicFields = "1";
      areaImpulse = "0";
      damageRadius = "0";
      invulnerable = "0";
      minDamageAmount = "0";
      radiusDamage = "0";
      hasGravity = false;
      isDynamic = false;
   };   
   $fixedB = new PhysicsShape() {
      playAmbient = "0";
      dataBlock = "PSCube";
      position = VectorAdd(%start,"3 6 -0.2");
      rotation = "1 0 0 0";
      canSave = "1";
      canSaveDynamicFields = "1";
      areaImpulse = "0";
      damageRadius = "0";
      invulnerable = "0";
      minDamageAmount = "0";
      radiusDamage = "0";
      hasGravity = true;
      isDynamic = true;
   };
   %a.jointAttach($fixedB,7);//FIXED 7
   ///////////////////////////////////////////////   
   %a = new PhysicsShape() {
      playAmbient = "0";
      dataBlock = "PSCube";
      position = VectorAdd(%start,"0 8 0");
      rotation = "1 0 0 0";
      canSave = "1";
      canSaveDynamicFields = "1";
      areaImpulse = "0";
      damageRadius = "0";
      invulnerable = "0";
      minDamageAmount = "0";
      radiusDamage = "0";
      hasGravity = false;
      isDynamic = false;
   };   
   $distanceB = new PhysicsShape() {
      playAmbient = "0";
      dataBlock = "PSCube";
      position = VectorAdd(%start,"3 8 0");
      rotation = "1 0 0 0";
      canSave = "1";
      canSaveDynamicFields = "1";
      areaImpulse = "0";
      damageRadius = "0";
      invulnerable = "0";
      minDamageAmount = "0";
      radiusDamage = "0";
      hasGravity = true;
      isDynamic = true;
   };
   %a.jointAttach($distanceB,8);//DISTANCE 8
   ///////////////////////////////////////////////   
   $d6A = new PhysicsShape() {
      playAmbient = "0";
      dataBlock = "PSCube";
      position = VectorAdd(%start,"0 10 0");
      rotation = "1 0 0 0";
      canSave = "1";
      canSaveDynamicFields = "1";
      areaImpulse = "0";
      damageRadius = "0";
      invulnerable = "0";
      minDamageAmount = "0";
      radiusDamage = "0";
      hasGravity = false;
      isDynamic = false;
   };   
   $d6B = new PhysicsShape() {
      playAmbient = "0";
      dataBlock = "PSCube";
      position = VectorAdd(%start,"3 10 0");
      rotation = "1 0 0 0";
      canSave = "1";
      canSaveDynamicFields = "1";
      areaImpulse = "0";
      damageRadius = "0";
      invulnerable = "0";
      minDamageAmount = "0";
      radiusDamage = "0";
      hasGravity = true;
      isDynamic = true;
   };
   $d6A.jointAttach($d6B,1);//D6 1
}

