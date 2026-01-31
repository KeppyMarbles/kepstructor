// Created by Keppy, 2026

// ---------------- Plugin ----------------

package map2csx {
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
    
    %exp = "constructor/map2csx/*.map";
    %convertCount = 0;
    for(%file = findFirstFile(%exp); %file !$= ""; %file = findNextFile(%exp)) {
      echo("Converting" SPC %file);
      scene.convertToMap(%file);
      %convertCount++;
    }
    
    MessageBoxOK("Done", "Converted" SPC %convertCount SPC "files to .csx");

    return tool.FUNC_OK(); 
  }
};

function CSceneManager::convertToMap(%this, %file) {
  %this.load(0, %file);
  %scene = %this.getObject(%this.getCount()-1);
  %csxfile = strreplace(%file, ".map", ".csx");
  %scene.save(%csxfile);
  %this.__closeStage2(%scene);
}


// ---------------- Initialization ----------------

tool.register("map2csx", tool.typeGeneric(), tool.RFLAG_NONE(), "map2csx" );

tool.setToolProperty("map2csx", "Icon", "standardicons/default");
tool.setToolProperty("map2csx", "Group", "Keppy's Plugins (Dev)");