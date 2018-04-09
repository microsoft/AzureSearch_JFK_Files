import * as React from "react";
import { ResultViewMode } from "../../view-model";
import IconButton from "material-ui/IconButton";

const style = require("./view-mode-toggler.style.scss");


interface ViewModeTogglerProps {
  resultViewMode: ResultViewMode;
  onChangeResultViewMode: (newMode: ResultViewMode) => void;
}

const toggleColor = (props: ViewModeTogglerProps) => (viewMode: ResultViewMode) => {
  return props.resultViewMode === viewMode ? "primary" : "inherit";
}

const notifyModeChanged = (props: ViewModeTogglerProps) => (newMode: ResultViewMode) => () =>{
  return props.onChangeResultViewMode(newMode);
}

export const ResultViewModeToggler = (props: ViewModeTogglerProps) => {
  const toggleColorFunc = toggleColor(props);
  const notifyModeChangedFunc = notifyModeChanged(props);
  return (
    <>
      <IconButton
        classes={{label: style.icon}}
        color={toggleColorFunc("grid")}
        onClick={notifyModeChangedFunc("grid")}
      >
        &#xe902;
      </IconButton>
      <IconButton
        classes={{label: style.icon}}
        color={toggleColorFunc("graph")}
        onClick={notifyModeChangedFunc("graph")}
      >
        &#xe904;
      </IconButton>
    </>
  );
}