import * as React from "react";

const logoSvg = require("../../../assets/svg/logoJFK.svg");

export const LogoComponent = ({classes}) => (
  <div className={classes.container}>
    <object className={classes.object}
      type="image/svg+xml"
      data={logoSvg}
    />
  </div>
);
