// esbuild-plugins.cjs
const path = require('path');
const glob = require('fast-glob');

const dynamicImportGlobPlugin = {
  name: 'dynamic-import-glob',
  setup(build) {
    build.initialOptions.external = [
      "@angular/*",
      "tslib"
    ];
    // Intercept import paths matching our "magic" string 'plugins:all'
    build.onResolve({ filter: /^plugins:all$/ }, args => ({
      path: args.path,
      namespace: 'dynamic-plugins-ns',
      pluginData: { importer: args.importer },
    }));

    // When esbuild tries to load a path in our namespace, we generate the content
    build.onLoad({ filter: /.*/, namespace: 'dynamic-plugins-ns' }, async (args) => {
      const importerDir = path.dirname(args.pluginData.importer);
      // process.cwd() in esbuild context is the project root where angular.json is
      const projectRoot = process.cwd();
      const internalPluginsRoot = path.resolve(projectRoot, 'src/app/plugins');
      const externalPluginsRoot = path.resolve(projectRoot, '../'); // Root of all C# projects

      console.log('[ESBUILD PLUGIN] Project root:', projectRoot);
      console.log('[ESBUILD PLUGIN] Internal plugins root:', internalPluginsRoot);
      console.log('[ESBUILD PLUGIN] External plugins root:', externalPluginsRoot);

      // Define glob patterns for both internal and external plugins
      const internalPluginPattern = '**/!(*.spec).module.ts'; // Exclude spec files
      const externalPluginPattern = 'GateKeeper.Plugin.*/Frontend/(portal|admin)/**/*.module.ts';

      // Fetch all .module.ts files from both locations
      let internalPluginPaths = await glob(internalPluginPattern, { cwd: internalPluginsRoot, absolute: true, onlyFiles: true });
      console.log('[ESBUILD PLUGIN] Raw internalPluginPaths from glob:', internalPluginPaths);
      let externalPluginPaths = await glob(externalPluginPattern, { cwd: externalPluginsRoot, absolute: true, onlyFiles: true });
      console.log('[ESBUILD PLUGIN] Raw externalPluginPaths from glob:', externalPluginPaths);

      let allPluginModulePaths = [...internalPluginPaths, ...externalPluginPaths];

      console.log('[ESBUILD PLUGIN] Discovered internal plugin paths:', internalPluginPaths);
      console.log('[ESBUILD PLUGIN] Discovered external plugin paths:', externalPluginPaths);
      console.log('[ESBUILD PLUGIN] All discovered plugin paths:', allPluginModulePaths);

      // This filtering logic might need to be adjusted based on the new structure
      // It assumes a structure like 'pluginName/pluginName.module.ts'
      const filteredPluginPaths = [];
      const seenPluginNames = new Set();

      // Process external plugins first to give them priority
      for (const modulePath of externalPluginPaths) {
        const normalizedPath = path.normalize(modulePath);
        const parts = normalizedPath.split(path.sep);
        const moduleFileName = parts[parts.length - 1].replace(/\.module\.ts$/, '');
        const dirName = parts[parts.length - 2];

        if (moduleFileName === dirName && !moduleFileName.includes('-routing')) {
          if (!seenPluginNames.has(dirName)) {
            filteredPluginPaths.push(modulePath);
            seenPluginNames.add(dirName);
          }
        }
      }

      // Process internal plugins, but only if an external plugin with the same name hasn't been added yet
      for (const modulePath of internalPluginPaths) {
        const normalizedPath = path.normalize(modulePath);
        const parts = normalizedPath.split(path.sep);
        const moduleFileName = parts[parts.length - 1].replace(/\.module\.ts$/, '');
        const dirName = parts[parts.length - 2];

        if (moduleFileName === dirName && !moduleFileName.includes('-routing')) {
          if (!seenPluginNames.has(dirName)) {
            filteredPluginPaths.push(modulePath);
            seenPluginNames.add(dirName);
          }
        }
      }

      console.log('[ESBUILD PLUGIN] Filtered plugin module paths:', filteredPluginPaths);

      const generatedMapEntries = filteredPluginPaths.map(modulePath => {
        // For external paths, we need to construct the key differently
        let pluginKey;
        if (modulePath.includes('GateKeeper.Plugin.')) {
            // Example path: .../GateKeeper.Plugin.Sample/Frontend/plugins/sample/sample.module.ts
            const match = modulePath.match(/Frontend[\/](portal|admin)[\/](?:plugins[\/])?([^\/]+)[\/]/);
            if (match && match[1] && match[2]) {
                const section = match[1]; // 'portal' or 'admin'
                const pluginFolder = match[2]; // This will be the plugin name directly
                pluginKey = `${section}/${pluginFolder}/${pluginFolder}`;
            } else {
            // Internal path logic remains the same
            const relativeFromInternalRoot = path.relative(internalPluginsRoot, modulePath);
            pluginKey = path.join('plugins', relativeFromInternalRoot.replace(/\.module\.ts$/, '')).replace(/\\/g, '/');
        }
}

        if (!pluginKey) {
            console.warn(`[ESBUILD PLUGIN] Could not determine plugin key for path: ${modulePath}`);
            return null; // Skip this entry
        }

        let relativePathToImport = path.relative(importerDir, modulePath).replace(/\\/g, '/').replace(/\.ts$/, '');
        if (!relativePathToImport.startsWith('.')) {
          relativePathToImport = './' + relativePathToImport;
        }
        console.log(`[ESBUILD PLUGIN] Generating map entry: Key='${pluginKey}', ImportPath='${relativePathToImport}'`);
        return `'${pluginKey}': () => import('${relativePathToImport}')`;
      }).filter(Boolean); // Filter out null entries

      const contents = `
        // This file is generated by esbuild-plugins.cjs at build time.
        // Importer: ${args.pluginData.importer}
        
        export const pluginLoaders = {
          ${generatedMapEntries.join(',\n')}
        };
      `;
      console.log('[ESBUILD PLUGIN] Generated content for plugins:all:\n', contents);

      return { contents, loader: 'js', resolveDir: importerDir };
    });
  },
};

module.exports = [dynamicImportGlobPlugin];
