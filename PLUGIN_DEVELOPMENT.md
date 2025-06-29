# Plugin Development Guide

This guide provides a comprehensive walkthrough for creating new plugins for the Gatekeeper application. The plugin system is designed to be modular, allowing you to extend both backend and frontend functionality in a self-contained way.

## Overview of the Plugin System

The plugin architecture connects a .NET backend with an Angular frontend.

*   **Backend Discovery:** The `GateKeeper.Server` project discovers plugins at startup. A plugin is a .NET class library that implements the `IPlugin` interface from the `GateKeeper.Plugin.Abstractions` project. Each plugin can register its own services and provides metadata about itself.
*   **API Manifest:** The server exposes an API endpoint (`/api/plugins/manifests`) that provides a list of all active plugins and their metadata. This metadata includes details needed by the frontend, such as the plugin's name, version, and crucially, the paths for its Angular module and routes.
*   **Frontend Dynamic Loading:** The Angular frontend's `PluginLoaderService` fetches the manifest when the application starts. It then uses this information to dynamically lazy-load the Angular modules for each active plugin and generate the necessary routes and navigation links in the user portal.

---

## Part 1: Creating the Backend Plugin

The backend part of a plugin is a .NET Class Library project.

### Step 1: Create the Plugin Project

1.  Create a new .NET Class Library project. For consistency, name it `GateKeeper.Plugin.YourPluginName`.
2.  Ensure the project targets a compatible .NET version (e.g., `net8.0`).
3.  Add a project reference to the `GateKeeper.Plugin.Abstractions` project. This gives you access to the `IPlugin` interface.

### Step 2: Implement the `IPlugin` Interface

Create a public class in your new project that implements the `IPlugin` interface. This class is the entry point for your backend plugin.

```csharp
// In GateKeeper.Plugin.YourPluginName/YourPluginName.cs
using GateKeeper.Plugin.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class YourPluginName : IPlugin
{
    // --- Metadata Properties ---

    public string Name => "My Awesome Plugin";
    public string Version => "1.0.0";
    public string Description => "This is a description of what my plugin does.";

    // --- Frontend Integration Properties ---

    public string DefaultRoutePath => "awesome-plugin";
    public string AngularModuleName => "AwesomePluginModule";
    public string AngularModulePath => "plugins/awesome-plugin/awesome-plugin.module";
    public string NavigationLabel => "Awesome Plugin";

    // --- Security ---

    public string RequiredRole => "User"; // Can be "Admin", "User", or a custom role. Set to null or empty for any authenticated user.

    // --- Service Configuration ---

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register any backend services your plugin needs.
        // Example:
        // services.AddScoped<IAwesomeService, AwesomeService>();
    }
}
```

#### `IPlugin` Property Details:

*   **`Name`, `Version`, `Description`**: Basic metadata for your plugin.
*   **`DefaultRoutePath`**: The URL segment for your plugin's base route under `/portal/`. For example, `awesome-plugin` results in the path `/portal/awesome-plugin`. This must be unique.
*   **`AngularModuleName`**: The **exact exported class name** of your plugin's main Angular module (e.g., `AwesomePluginModule`).
*   **`AngularModulePath`**: The path to your plugin's main Angular module file, relative to `gatekeeper.client/src/app/` and **without the `.ts` extension**. This is critical for the frontend's dynamic loader. Example: `plugins/awesome-plugin/awesome-plugin.module`.
*   **`NavigationLabel`**: The text that will appear in the portal's sidebar navigation for your plugin.
*   **`RequiredRole`**: The role a user must have to access this plugin. This is enforced by the `RoleGuard` on the frontend. More details in the "Plugin Security" section below.
*   **`ConfigureServices`**: A method where you can register any services specific to your plugin's backend logic using the provided `IServiceCollection`.

### Step 3: Add a Reference to the Server

In the `GateKeeper.Server.csproj` file, add a project reference to your new plugin project. This ensures the server loads your plugin's assembly at startup.

```xml
<ItemGroup>
  <ProjectReference Include="..\GateKeeper.Plugin.YourPluginName\GateKeeper.Plugin.YourPluginName.csproj" />
</ItemGroup>
```

---

## Part 2: Creating the Frontend Plugin

To achieve better encapsulation, the Angular frontend code for a plugin can reside directly within its corresponding C# plugin project. This section outlines how to set up your frontend plugin in this manner.

### Step 1: Create the Frontend Directory and Files within the C# Plugin Project

1.  Inside your C# plugin project (e.g., `GateKeeper.Plugin.YourPluginName`), create a new folder named `Frontend`.
2.  Inside the `Frontend` folder, create a subfolder structure like `plugins/your-plugin-name/`.
3.  Place your Angular module, components, routing, and other related files within this `plugins/your-plugin-name/` directory.

    Example structure:
    ```
    GateKeeper.Plugin.YourPluginName/
    ├── YourPluginName.cs
    ├── Frontend/
    │   └── plugins/
    │       └── your-plugin-name/
    │           ├── your-plugin-name.module.ts
    │           ├── your-plugin-name-routing.module.ts
    │           ├── your-plugin-name.component.ts
    │           ├── your-plugin-name.component.html
    │           ├── your-plugin-name.component.scss
    │           └── components/ (optional, for sub-components)
    └── GateKeeper.Plugin.YourPluginName.csproj
    ```

### Step 2: Define the Angular Module and Components (Standalone Recommended)

For new plugins, it is highly recommended to use **standalone components** and modules where applicable. This simplifies the Angular module structure.

**`your-plugin-name.component.ts` (Example Standalone Component):**
```typescript
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
// Import any Angular Material modules or other dependencies directly here
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-your-plugin',
  templateUrl: './your-plugin-name.component.html',
  styleUrls: ['./your-plugin-name.component.scss'],
  standalone: true, // Mark as standalone
  imports: [
    CommonModule, // Required for directives like *ngIf, *ngFor
    ReactiveFormsModule, // Required for form handling
    MatCardModule, // Example Angular Material module
    // ... other modules your component needs
  ]
})
export class YourPluginNameComponent implements OnInit {
  // ... component logic ...
}
```

**`your-plugin-name.module.ts` (Example Module for Routing/Lazy Loading):**
```typescript
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

import { YourPluginNameComponent } from './your-plugin-name.component';
import { YourPluginNameRoutingModule } from './your-plugin-name-routing.module';

@NgModule({
  declarations: [
    // Standalone components are NOT declared here
  ],
  imports: [
    CommonModule,
    RouterModule,
    YourPluginNameRoutingModule, // This sets up the routes for this lazy-loaded module
    YourPluginNameComponent // Import the standalone component directly
  ]
})
export class YourPluginNameModule { } // This class name must match AngularModuleName in the C# plugin
```

### Step 3: Define the Plugin's Routes

This routing module defines the routes *within* your plugin. The base path is determined by the `DefaultRoutePath` from the backend.

**`your-plugin-name-routing.module.ts`:**
```typescript
import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { YourPluginNameComponent } from './your-plugin-name.component';

const routes: Routes = [
  {
    path: '', // This is the base path, e.g., if plugin path is 'awesome-plugin', this is '/portal/awesome-plugin'
    component: YourPluginNameComponent,
    // children: [ ... you can define sub-routes here ... ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class YourPluginNameRoutingModule { }
```

### Step 4: Update the C# Plugin Project File (`.csproj`)

To ensure the Angular files within your C# project are recognized and included in the build output (so they can be discovered by the frontend), you need to add a `Content` item group to your C# plugin's `.csproj` file.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <!-- ... existing ItemGroups and PropertyGroups ... -->

  <ItemGroup>
    <Content Include="Frontend\**\*.*">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
  </ItemGroup>

  <!-- ... existing Targets ... -->

</Project>
```

---

## Part 3: Frontend Application Configuration for External Plugins

To enable the `gatekeeper.client` Angular application to discover and load plugins whose frontend code resides in external C# projects, specific configurations are required in `tsconfig.json` and `esbuild-plugins.cjs`.

### 1. `tsconfig.json` Updates

Modify `gatekeeper.client/tsconfig.json` to correctly resolve paths for external plugins and core Angular modules.

```json
{
  "compileOnSave": false,
  "compilerOptions": {
    "outDir": "./dist/out-tsc",
    "strict": true,
    "noImplicitOverride": true,
    "noPropertyAccessFromIndexSignature": true,
    "noImplicitReturns": true,
    "noFallthroughCasesInSwitch": true,
    "skipLibCheck": true,
    "isolatedModules": true,
    "esModuleInterop": true,
    "experimentalDecorators": true,
    "moduleResolution": "bundler",
    "importHelpers": true,
    "target": "ES2022",
    "module": "ES2022",
    "baseUrl": ".", // Set base URL to project root
    "paths": {
      "@plugins/*": [
        "../GateKeeper.Plugin.*/Frontend/plugins/*", // Wildcard for all plugin projects
        "./src/app/plugins/*" // Original internal plugins
      ],
      "@app/*": [
        "./src/app/*" // Alias for main application's src/app
      ],
      "@angular/*": [
        "./node_modules/@angular/*" // Explicitly map Angular modules
      ],
      "tslib": [
        "./node_modules/tslib" // Explicitly map tslib
      ]
    },
    "typeRoots": [
      "./node_modules/@types" // Ensure TypeScript types are found
    ]
  },
  "angularCompilerOptions": {
    "enableI18nLegacyMessageIdFormat": false,
    "strictInjectionParameters": true,
    "strictInputAccessModifiers": true,
    "strictTemplates": true
  }
}
```

### 2. `tsconfig.app.json` Updates

Modify `gatekeeper.client/tsconfig.app.json` to include the TypeScript files from the external plugin projects in the compilation.

```json
{
  "extends": "./tsconfig.json",
  "compilerOptions": {
    "outDir": "./out-tsc/app",
    "types": []
  },
  "files": [
    "src/main.ts"
  ],
  "include": [
    "src/**/*.ts",
    "../GateKeeper.Plugin.Profile/Frontend/**/*.ts", // Include TypeScript files from Profile plugin
    "../GateKeeper.Plugin.Sample/Frontend/**/*.ts" // Include TypeScript files from Sample plugin
  ],
  "exclude": [
    "src/**/*.spec.ts"
  ]
}
```

### 3. `esbuild-plugins.cjs` Updates

Modify `gatekeeper.client/esbuild-plugins.cjs` to discover plugins from both the internal `src/app/plugins` directory and the `Frontend/plugins` directories within your C# plugin projects. It also explicitly marks core Angular modules and `tslib` as external for esbuild.

```javascript
// esbuild-plugins.cjs
const path = require('path');
const glob = require('fast-glob');

const dynamicImportGlobPlugin = {
  name: 'dynamic-import-glob',
  setup(build) {
    // Mark Angular and tslib as external to prevent bundling issues
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
      const projectRoot = process.cwd();
      const internalPluginsRoot = path.resolve(projectRoot, 'src/app/plugins');
      const externalPluginsRoot = path.resolve(projectRoot, '../'); // Root of all C# projects

      console.log('[ESBUILD PLUGIN] Project root:', projectRoot);
      console.log('[ESBUILD PLUGIN] Internal plugins root:', internalPluginsRoot);
      console.log('[ESBUILD PLUGIN] External plugins root:', externalPluginsRoot);

      // Define glob patterns for both internal and external plugins
      const internalPluginPattern = '**/!(*.spec).module.ts'; // Exclude spec files
      const externalPluginPattern = 'GateKeeper.Plugin.*/Frontend/plugins/**/*.module.ts';

      // Fetch all .module.ts files from both locations
      let internalPluginPaths = await glob(internalPluginPattern, { cwd: internalPluginsRoot, absolute: true, onlyFiles: true });
      let externalPluginPaths = await glob(externalPluginPattern, { cwd: externalPluginsRoot, absolute: true, onlyFiles: true });

      let allPluginModulePaths = [...internalPluginPaths, ...externalPluginPaths];

      console.log('[ESBUILD PLUGIN] Discovered internal plugin paths:', internalPluginPaths);
      console.log('[ESBUILD PLUGIN] Discovered external plugin paths:', externalPluginPaths);
      console.log('[ESBUILD PLUGIN] All discovered plugin paths:', allPluginModulePaths);

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
            const match = modulePath.match(/plugins[\\\/]([^\\\/]+)[\\\/][^\\\/]+\.module\.ts$/);
            if (match && match[1]) {
                pluginKey = `plugins/${match[1]}/${match[1]}`;
            }
        } else {
            // Internal path logic remains the same
            const relativeFromInternalRoot = path.relative(internalPluginsRoot, modulePath);
            pluginKey = path.join('plugins', relativeFromInternalRoot.replace(/\.module\.ts$/, '')).replace(/\\/g, '/');
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
```

---

## Part 4: Plugin Security (Role-Based Access)

Gatekeeper uses a layered guard system to protect plugin routes. When a user navigates to a plugin route, the guards run in this order:

1.  **`AuthGuard`**: Ensures the user is logged in.
2.  **`RoleGuard`**: Ensures the logged-in user has the necessary role to access the plugin.

This system is configured automatically for all plugins in `app-routing.module.ts`.

### How to Secure Your Plugin

Securing your plugin is done entirely from the backend C# plugin class by setting the `RequiredRole` property.

```csharp
// In YourPluginName.cs
public class YourPluginName : IPlugin
{
    // ... other properties ...
    public string RequiredRole => "SuperEditor"; // Only users with the "SuperEditor" role can access.
}
```

The `RoleGuard` on the frontend reads this value from the plugin manifest and enforces it.

#### `RequiredRole` Options:

*   **Custom Role (e.g., `"SuperEditor"`, `"Manager"`)**: Only users who have been assigned this specific role can access the plugin.
*   **`"User"`**: Users with the `"User"` role can access. Additionally, users with the `"Admin"` role are automatically granted access as an override.
*   *   **`"Admin"`**: Only users with the `"Admin"` role can access.
*   **`null` or `""` (Empty String)**: If you set the role to `null` or an empty string, the `RoleGuard` will allow access to *any authenticated user* (i.e., any user who has passed the `AuthGuard`). This is useful for plugins that should be available to all logged-in users.

When creating a new plugin, simply decide on its access level and set the `RequiredRole` property accordingly. No frontend changes are needed to apply the security. Remember to ensure that the roles you specify exist in the system and are assigned to the correct users.

---

## Part 5: Build and Verify

1.  **Rebuild the Solution**: Perform a full rebuild of the Visual Studio solution to ensure the new plugin project is compiled and its DLL is copied to the server's output directory.
2.  **Run the Application**: Start both the backend server and the Angular development server.
3.  **Check the Backend**:
    *   Look at the server's console logs to confirm it discovered and loaded your new plugin.
    *   Navigate to the `/api/plugins/manifests` endpoint in your browser to see your plugin's metadata listed in the JSON response.
4.  **Check the Frontend**:
    *   Log in as a user who has the `RequiredRole` for your plugin.
    *   Check the portal's navigation menu for your plugin's `NavigationLabel`.
    *   Click the link and verify that your plugin's main component loads correctly at its route (e.g., `/portal/awesome-plugin`).
    *   Log in as a user who *does not* have the required role and confirm that the navigation link is not visible and that they are redirected if they try to access the URL directly.