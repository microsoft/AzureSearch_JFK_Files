import * as React from "react";
import { Logo } from "./logo";


export const LogoComponent = ({ classes }) => (
  <div className={classes.container}>
    <Logo
      className={classes.object}
    />
  </div>
);
