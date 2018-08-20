const fs = require('fs');

function deploymentOutput(input)
{
    // grab output variables
    const outputs = input.properties.outputs;
    const settings = {};
    for (const key in outputs) {
        settings[key] = outputs[key].value;
    }

    return settings;
}

const output = deploymentOutput(JSON.parse(fs.readFileSync(0).toString()));
fs.writeSync(1, JSON.stringify(output, null, 2));
