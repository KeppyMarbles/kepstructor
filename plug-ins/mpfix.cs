// Created By Keppy, 2025

// Takes a selection of path nodes, triggers, and a door elevator and puts them at
// the end of the entity list in the correct order.

// ---------------- Plugin ----------------

package MPFix {
  function Plugin::Activate(%this, %version, %inst, %static) {
    if(%version != 1) {
      return tool.FUNC_BADVERSION();
    }
    
    %inst.flagsInterface = tool.IFLAG_NONE();
    %inst.flagsApply = tool.AFLAG_NONE();
    
    %map = scene.getCurrentMap();
    %scene = scene.getCurrent();
    
    %brushSelection = %map.getSelectedBrushes();
    %elevatorID = "";
    %triggerCount = 0;
    %brushCount = 0;
    for (%i = 0; %i < getWordCount(%brushSelection); %i++) {
      %brushID = getWord(%brushSelection, %i);
      %entityID = %map.getBrushOwner(%brushID);
      switch$ (%map.getEntityClassname(%entityID)) {
        case "Door_Elevator":
          if(%elevatorID !$= "" && %entityID !$= %elevatorID) {
            tool.activateErrorMsg = "Cannot be used with more than 1 MP at a time";
            return tool.FUNC_BADGENERAL();
          }
          %elevatorID = %entityID;
          %brushes[%brushCount] = %brushID;
          %brushCount++;
          
        case "trigger":
          %triggers[%triggerCount] = %entityID;
          %triggerBrushes[%entityID] = %brushID;
          %triggerCount++;
      }
    }
    %shapeSelection = %scene.getSelectedShapes();
    %pathNodeCount = 0;
    for (%i = 0; %i < getWordCount(%shapeSelection); %i++) {
      %shapeID = getWord(%shapeSelection, %i);
      %entityID = %scene.getShapePointEntityID(%shapeID);
      switch$ (%map.getEntityClassname(%entityID)) {
        case "path_node":
          %pathNodes[%pathNodeCount] = %entityID;
          %pathNodeShapes[%entityID] = %shapeID;
          %pathNodeCount += 1;
      }
    }
    
    if(%elevatorID $= "") {
      tool.activateErrorMsg = "No Door_Elevator was selected";
      return tool.FUNC_BADGENERAL();
    }
    
    if(%pathNodeCount < 2) {
      tool.activateErrorMsg = "You need to select at least 2 path_nodes";
      return tool.FUNC_BADGENERAL();
    }
    
    getCurrentUndoManager().enableSnapshots(false);
    
    %newDoorID = %map.duplicateEntity(%elevatorID);
    for(%i = 0; %i < %brushCount; %i++) {
      %map.setBrushOwner(%brushes[%i], %newDoorID);
    }
    %map.removeEntity(%elevatorID);
    
    for(%i = 0; %i < %pathNodeCount; %i++) {
      %newNodeID = %map.duplicateEntity(%pathNodes[%i]);
      %scene.changeShapePointEntityID(%pathNodeShapes[%pathNodes[%i]], %newNodeID);
      %map.removeEntity(%pathNodes[%i]);
    }
    
    for(%i = 0; %i < %triggerCount; %i++) {
      %newTriggerID = %map.duplicateEntity(%triggers[%i]);
      %map.setBrushOwner(%triggerBrushes[%triggers[%i]], %newTriggerID);
      %map.removeEntity(%triggers[%i]);
    }
    
    getCurrentUndoManager().enableSnapshots(true);
    
    MessageBoxOK("Success", "Created a moving platform group with" SPC %pathNodeCount SPC "path nodes and" SPC %triggerCount SPC "triggers.");

    return tool.FUNC_OK();
  }
};

// ---------------- Other ----------------

package stopSpamming {
  function EntityPropertiesForm::processTick(%this) {
    return;
  }
};

// ---------------- Initialization ----------------

activatePackage(stopSpamming);

tool.register("MPFix", tool.typeGeneric(), tool.RFLAG_NONE(), "MP Entity Assist" );

tool.setToolProperty("MPFix", "Icon", "mpfix");
tool.setToolProperty("MPFix", "Group", "Keppy's Plugins");