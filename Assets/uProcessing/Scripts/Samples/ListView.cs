using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using uP5;

public class ListView : uProcessing
{

    List<ListItem> list;

    protected override void setup()
    {
        size(200 * displayAspectW, 200, P2D);

        list = new List<ListItem>();
        list.Add(new ListItem("Menu"));
        list.Add(new ListItem("Shapes"));
        list.Add(new ListItem("JsonData"));
        list.Add(new ListItem("Performance"));
        list.Add(new ListItem("Tweens"));
        list.Add(new ListItem("Sounds"));
        list.Add(new ListItem("Dialog"));
        list.Add(new ListItem("Pteridophyte"));
    }

    protected override void draw()
    {
        background(128);

        uiColor(color(0), color(100, 200, 100));
        uiTextAlign(LEFT);

        layer2D();
        noRecycle();
        listView(list, 10, 10, width - 10 * 2, height - 10 * 2, 20);
        recycle();
    }

    protected override void buttonClick(string groupName, string name, PGameObject obj)
    {
        loadScene(name);
    }
}
