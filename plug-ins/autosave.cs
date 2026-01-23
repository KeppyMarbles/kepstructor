// Created by Keppy, 2025

// ---------------- Plugin ----------------

package AutoSave {
  function Plugin::Activate(%this, %version, %inst, %static) {
    if(%version != 1) {
      return tool.FUNC_BADVERSION();
    }
    
    %plugin = new ScriptObject();
    %plugin.static = %static;
    
    %plugin.dirty = tool.DIRTY_NONE();
    %plugin.active = true;
    %plugin.update = tool.EDIT_DONOTHING();

    %inst.instance = %plugin;
    %inst.flagsInterface = tool.IFLAG_NOTOOLCURSOR();

    return tool.FUNC_OK();
  }
  
  function Plugin::Interface(%this, %inst, %form) {
    %form.defineTitle("Autosave");
    %form.addField(0, "Enabled", "checkbox");
    %form.addField(1, "Interval (seconds)", "numericinteger");
    %form.setFieldMinLimit(1, 1);
    %form.addField(2, "Save Session", "checkbox");
  }
  
  function Plugin::InterfaceGet(%this, %inst, %id) {
    switch(%id) {
      case 0:
        return $pref::Autosave::Enabled;
      case 1:
        return $pref::Autosave::Interval;
      case 2:
        return $pref::Autosave::SaveSession;
    }
  }
  
  function Plugin::InterfaceSet(%this, %inst, %id, %value) {
    switch(%id) {
      case 0:
        $pref::Autosave::Enabled = %value;
        updateSaveSession();
      case 1:
        $pref::Autosave::Interval = %value;
        scene.autoSave();
      case 2:
        $pref::Autosave::SaveSession = %value;
        updateSaveSession();
    }
  }
};

// ---------------- Autosave ----------------

function startAutosave() {
  cancel($Autosave::schedule);
  if($pref::Autosave::Interval > 0)
    $Autosave::schedule = scene.schedule($pref::Autosave::Interval * 1000, "autoSave");
}

function backupScene(%sceneFile) {
  echo("AutoSave: backing up scene" SPC %sceneFile);
  pathCopy(%sceneFile, $constructorPath @ "/autosave/" @ fileName(%sceneFile) @ ".old", false);
}

function updateSaveSession() {
  deactivatePackage(SaveSession);
  if($pref::Autosave::Enabled && $pref::Autosave::SaveSession)
    activatePackage(SaveSession);
}

function CSceneManager::autoSave(%this, %currentOnly) {
  if(!$pref::Autosave::Enabled)
    return;
  
  %dummyPath = $constructorPath @ "/autosave/.dummy";
  
  // Make sure the autosave folder exists
  if(!PlatformIsFile(%dummyPath)) {
    %fo = new FileObject();
    if(%fo.openForWrite(%dummyPath)) {
      %fo.close();
      %fo.delete();
    }
    fileDelete(%dummyPath);
  }
  
  %saved = %this.saveScenes(%currentOnly);
  
  if(!%saved)
    echo("AutoSave: nothing to save");
  
  // Save session data into prefs
  if($pref::Autosave::SaveSession)
    %this.saveSession();
  
  // Again
  startAutosave();
}

function CSceneManager::saveScenes(%this, %currentOnly) {
  // Save (all?) scenes into autosave folder
  for(%i = 0; %i < %this.getCount(); %i++) {
    %scene = %this.getObject(%i);
    
    if(%currentOnly && %scene != %this.getCurrent())
      continue;
    
    // Check brush count
    if(%scene.getDetailLevelMap(0).getNumEntityChildren(0) == 0)
      continue;
    
    %sceneFile = %scene.getName();
    %sceneName = fileBase(%sceneFile);
    
    // Get path for unsaved scene
    if(%sceneFile $= %sceneName) {
      %path = $Autosave::lastSave[%sceneName];
      if(!PlatformIsFile(%path)) {
        // Create a new CSX file, with a timestamp for uniqueness and use it for this session
        %time_str = strreplace(strreplace(strreplace(getLocalTime(), " ", "_"), "/", "-"), ":", "-");
        %path = $constructorPath @ "/autosave/" @ %sceneName @ "_" @ %time_str @ ".csx";
        $Autosave::lastSave[%sceneName] = %path;
      }
    }
    // Or already saved scene
    else {
      %path = %sceneFile;
    }
    echo("AutoSave: saving" SPC %path);
    
    // Perform the save
    activatePackage(BeforeAutoSave);
    
    if(strstr(%sceneFile, ".map") != -1)
      %scene.getDetailLevelMap(0).exportMap(%path);
    else
      %scene.save(%path);
    
    deactivatePackage(BeforeAutoSave);
    
    %saved = true;
  }
  return %saved;
}

function CSceneManager::saveSession(%this) {
  for(%i = 0; %i < $pref::Autosave::lastOpenedSceneCount; %i++) {
    $pref::Autosave::lastOpenedScenes[%i] = "";
  }
  $pref::Autosave::lastOpenedSceneCount = %this.getCount();
  
  // Add all opened scenes to prefs
  for (%i = 0; %i < %this.getCount(); %i++) {
    %scene = %this.getObject(%i);
    %sceneFile = %scene.getFileName();
    if(%sceneFile $= fileBase(%sceneFile)) {
      %sceneFile = $Autosave::lastSave[%sceneFile];
    }
    $pref::Autosave::lastOpenedScenes[%i] = %sceneFile;
  }
}

function CSceneManager::loadPreviousSession(%this) {
  if($prevSessionLoaded)
    return;
  $prevSessionLoaded = true;
  
  // Opened saved scenes
  for(%i = 0; %i < $pref::Autosave::lastOpenedSceneCount; %i++) {
    %sceneFile = $pref::Autosave::lastOpenedScenes[%i];
    if(%sceneFile !$= "")
      %this.load(0, %sceneFile);
  }
  if($pref::Autosave::lastOpenedSceneCount > 1) {
    // Close default scene
    %this.__closeStage2(%this.getObject(0));
  }
}

// ---------------- Packages ----------------

package ImproveSave {
  function saveLightingData(%scene, %scene_filename) {
    return; // Nuke this function
  }
  function InteriorMap::getBrushScale(%brushID) {
    return; // Stop spamming the console
  }
  function CSceneManager::load(%this, %callback, %filename, %noFileCheck)
  {
    if($pref::Autosave::Enabled) {
      for(%i = 0; %i < %this.getCount(); %i++) {
        if(%filename $= %this.getObject(%i).getName()) {
          MessageBoxOK("Error", "Can't load" SPC %filename SPC "while it's already open, as this could cause issues with Autosave.");
          return;
        }
      }
    }
    Parent::load(%this, %callback, %filename, %noFileCheck);
  }
};

package BeforeAutoSave {
  function CScene::__setFilename(%filename) {
    return; // Prevent changing the scene's file to the one in the autosave folder
  }
};

package SaveSession {
  // Edit: autosave current then close the scene without prompting
  function CSceneManager::close(%this) {
    %this.autoSave(true);
    
    tool.finish();
    %scene = %this.getCurrent();
    if (%scene != -1)
      %this.__closeStage2(%scene);
  }
  
  // Edit: autosave all then close everything without prompting
  function CSceneManager::closeAll(%this, %quit) {
    %this.autoSave();
    
    tool.finish();
    %count = %this.getCount();

    for (%i = 0; %i < %count; %i++) {
      %this.__closeAllStage2(%this.getObject(0));
    }
    if (%quit) {
      ShutdownSequenceNext();
    }
    else {
      select.clear();
      %this.newScene();
    }
  }
  
  // Edit: Update the .old file
  function CSceneManager::save(%this) {
    Parent::save(%this);
    backupScene(%this.getCurrent().getFileName());
  }
};

// ---------------- Initialization ----------------

tool.register("AutoSave", tool.typeDialog(), tool.RFLAG_NONE(), "AutoSave" );

tool.setToolProperty("AutoSave", "Icon", "autosave");
tool.setToolProperty("AutoSave", "Group", "Keppy's Plugins");

// Defaults
if($pref::Autosave::Enabled $= "")
  $pref::Autosave::Enabled = true;

if($pref::Autosave::SaveCurrentOnly $= "")
  $pref::Autosave::SaveCurrentOnly = false;

if($pref::Autosave::Interval $= "")
  $pref::Autosave::Interval = 30;

if($pref::Autosave::SaveSession $= "")
  $pref::Autosave::SaveSession = true;

updateSaveSession();

// Start
activatePackage(ImproveSave);
startAutosave();

if($pref::Autosave::SaveSession)
  scene.schedule(1000, "loadPreviousSession");

//
$constructorPath = filePath(strreplace($Game::argv[0], "\\", "/"));

function AutoSave::re() {
  fileDelete($constructorPath @ "/" @ $Con::File @ ".dso");
	exec($Con::File);
}