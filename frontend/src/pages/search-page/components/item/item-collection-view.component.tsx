import * as React from "react";
import { ItemComponent } from "./item.component";
import { ItemCollection, Item } from "../../view-model";

const style = require("./item-collection-view.style.scss");


interface ItemViewProps {
  items?: ItemCollection;
  activeSearch?: string;
  onClick?: (item: Item) => void;
}

export class ItemCollectionViewComponent extends React.Component<ItemViewProps, {}> {
  public constructor(props) {
    super(props);
  }
  
  public render() {
    return (    
      <div className={style.container}>
        { this.props.items ? 
          this.props.items.map((child, index) => (
            <ItemComponent
              item={child}
              activeSearch={this.props.activeSearch}
              onClick={this.props.onClick}
              key={index}
            />
          ))
        : null }
      </div>
    );
  }  
}
