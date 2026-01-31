// Created by Keppy, 2026

// ---------------- Plugin ----------------

package HotReload {
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
    %form.defineTitle("Hot Reload");
    %form.addField(0, "Enabled", "checkbox");
  }
  
  function Plugin::InterfaceGet(%this, %inst, %id) {
    switch(%id) {
      case 0:
        return $pref::HotReload::Enabled;
    }
  }
  
  function Plugin::InterfaceSet(%this, %inst, %id, %value) {
    switch(%id) {
      case 0:
        $pref::HotReload::Enabled = %value;
        toggleHotReload();
    }
  }
};

// ---------------- Hot Reload ----------------

function toggleHotReload() {
  if($pref::HotReload::Enabled) {
    %patterns = "*.cs;*.gui";
    %i = 0;
    %exp = "*/plug-ins/*.cs";
    for(%file = findFirstFile(%exp); %file !$= ""; %file = findNextFile(%exp)) {
      $Con::HotReload::file[%i] = %file;
      %i++;
    }
    $Con::HotReload::fileCount = %i;
    echo("Watching" SPC %i SPC "files");
    hotReloadLoop();
  }
}

function hotReloadLoop() {
  if(!$pref::HotReload::Enabled) {
    echo("Stopped file watcher");
    return;
  }
  
  %changedFilesList = "";
  for(%i = 0; %i < $Con::HotReload::fileCount; %i++) {
    %file = $Con::HotReload::file[%i];
    %currentMod = getFileCRC(%file);
    if($Con::HotReload::LastMod[%file] !$= "" && $Con::HotReload::LastMod[%file] !$= %currentMod)
      %changedFilesList = %changedFilesList TAB %file;
    $Con::HotReload::LastMod[%file] = %currentMod;
  }
  
  %count = getFieldCount(%changedFilesList);
  for(%i = 1; %i < %count; %i++) {
    %fileToReload = getField(%changedFilesList, %i);
    echo("File changed:" SPC %fileToReload);
    fileDelete($constructorPath @ "/" @ %fileToReload @ ".dso");
    exec(%fileToReload);
  }
  
  if($ScriptError !$= "") {
    MessageBoxOk("Script Error", $ScriptError);
    $ScriptError = "";
  }
  
  cancel($Con::HotReload::Schedule);
  $Con::HotReload::Schedule = schedule(500, 0, hotReloadLoop);
}

// ---------------- Initialization ----------------

tool.register("HotReload", tool.typeDialog(), tool.RFLAG_NONE(), "Hot Reload Plugins" );

tool.setToolProperty("HotReload", "Icon", "standardicons/default");
tool.setToolProperty("HotReload", "Group", "Keppy's Plugins (Dev)");

// Start
toggleHotReload();

//
$constructorPath = filePath(strreplace($Game::argv[0], "\\", "/"));