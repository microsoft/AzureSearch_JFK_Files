import * as React from "react";
import IconButton from "material-ui/IconButton";
import MuiDialog, { DialogTitle, DialogContent, DialogContentText, DialogProps } from "material-ui/Dialog";
import CloseIcon from "material-ui-icons/Close";
import Typography from "material-ui/Typography";
import { withTheme } from "material-ui/styles";
import { cnc } from "../../../../util";
import { LinkComponent } from './link.component';
const styles = require('./dialog.styles.scss');
const jfkFilesScenario = require('../../../../assets/img/jfk-files-scenario.png');

const Dialog: React.StatelessComponent<DialogProps> = ({ ...props }) => {
  return (
    <MuiDialog {...props} className={cnc(props.className, styles.dialog)} classes={{ paper: styles.content }}>
      <DialogTitle>
        <div className={styles.titleContainer}>
          <Typography variant="title" classes={{ title: styles.title }}>JFKFiles Cognitive Search Pattern</Typography>
          <IconButton onClick={props.onClose}>
            <CloseIcon />
          </IconButton>
        </div>
      </DialogTitle>
      <DialogContent>
        <DialogContentText>
          <span className={styles.block}>
            In this JFK Files scenario demo, you will explore how you can leverage Azure Cognitive Services and Search to
            implement the Cognitive Search pattern in an application, using the released documents from
            The President John F. Kennedy Assassination Records Collection.
          </span>
          <span className={styles.block}>
            <span>You can find more information </span>
            <LinkComponent to="//azure-scenarios-experience.azurewebsites.net/search-ai.html">here</LinkComponent>
          </span>
          <span className={styles.block}>Here's the architecture used for JFK files scenario:</span>
          <img src={jfkFilesScenario} alt="JFK scenario" className={styles.img} />
          <span className={styles.block}>
            <span>You can find the source code </span>
            <LinkComponent to="//github.com/Microsoft/AzureSearch_JFK_Files">here</LinkComponent>
          </span>
        </DialogContentText>
      </DialogContent>
    </MuiDialog>
  );
}

export const DialogComponent = withTheme()(Dialog);
