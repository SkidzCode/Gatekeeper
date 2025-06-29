# GateKeeper Angular Plugin System Details

## 1. Introduction and Purpose

This document details the architecture and workflow of the plugin system used in the GateKeeper Angular client application. The primary goal of this system is to allow for the dynamic discovery, loading, and routing of external or internal Angular modules (plugins) at runtime, without needing to recompile the main application for each new plugin. This provides a flexible and extensible way to add new features and sections to the application.

## 2. Core Concepts

The plugin system revolves around a few core concepts:

*   **Plugin Manifests:** JSON objects that describe each plugin, providing metadata like its name, module paths, route paths, and required roles. These are fetched from a backend API at runtime.
*   **Build-Time Discovery & Module Map (`pluginLoaders`):** A build process (`esbuild` with custom scripting) scans the project for plugin modules at build time and creates a map. This map (referred to as `pluginLoaders`) associates a unique `pluginKey` with a function that can dynamically import the plugin's main Angular module.
*   **Dynamic Route Generation:** At application startup, the Angular router's configuration is dynamically updated based on the fetched plugin manifests. Routes are created to lazy-load plugin modules using the `pluginLoaders` map.
*   **Lazy Loading:** Plugin modules are only loaded into the browser when a user navigates to a route associated with that plugin, improving initial application load time.

## 3. System Components

### 3.1. Backend API: `/api/plugins/manifests`

*   **Responsibility:** Serves an array of plugin manifest objects (see `AngularPluginInfo` below).
*   **Interaction:** The `PluginLoaderService` in the Angular application fetches data from this endpoint during application initialization.
*   **Note:** The accuracy and completeness of the data provided by this API are crucial for the correct functioning of the plugin system on the client side.

### 3.2. Build Script: `esbuild-plugins.cjs` (Build-Time Processing)

*   **Responsibility:** This Node.js script runs as part of the Angular build process (integrated with `esbuild`). Its main tasks related to plugins are:
    1.  **Discovery:** Scan predefined directories for plugin frontend entry points (Angular modules, e.g., `*.module.ts`).
    2.  **Key Generation:** For each discovered plugin module, generate a unique `pluginKey`. The current logic for this key (as of this document's writing) is:
        *   It parses the module's file path using a regex: `modulePath.match(/Frontend[\/](portal|admin)[\/](?:plugins[\/])?([^\/]+)[\/]/)`.
        *   `section`: Captures `portal` or `admin`.
        *   `pluginFolder`: Captures the main folder name of the plugin (e.g., `profile`, `sample-admin`).
        *   The `pluginKey` is constructed as: `` `${section}/${pluginFolder}/${pluginFolder}` ``.
        *   Example: A module at `.../Frontend/portal/profile/profile.module.ts` results in `pluginKey = "portal/profile/profile"`.
    3.  **`pluginLoaders` Map Generation:** Create a JavaScript/TypeScript module (aliased as `plugins:all` in the Angular app) that exports a `pluginLoaders` constant. This constant is an object where keys are the generated `pluginKey`s and values are functions that perform a dynamic import of the corresponding plugin module.
        *   Example entry in the generated `pluginLoaders`:
            ```javascript
            'portal/profile/profile': () => import('../../../GateKeeper.Plugin.Profile/Frontend/portal/plugins/profile/profile.module')
            ```

### 3.3. Manifest Structure: `AngularPluginInfo` Interface

This interface (implicitly defined by usage, ideally would be a formal `interface` or `type` in TypeScript) describes the structure of objects fetched from `/api/plugins/manifests`. Key fields include:

*   `name: string`: Display name of the plugin (e.g., "User Profile Plugin").
*   `angularModulePath: string`: The path to the plugin's main Angular module file, relative to a base path understood by `esbuild` and used for client-side `pluginKey` generation. Example: `"profile/profile.module"`. **Crucial for portal plugins.**
*   `angularModuleName: string`: The name of the exported Angular module class within the file specified by `angularModulePath`. Example: `"ProfileModule"`. **Crucial for loading the correct module export.**
*   `routePath: string`: The intended base route path for the portal version of the plugin. Example: `"portal/profile"`. Used to derive the final route segment.
*   `navigationLabel: string`: Label used for UI elements like navigation links.
*   `requiredRole: string | null`: Role required to access the portal plugin.
*   `adminAngularModulePath: string | null`: Similar to `angularModulePath`, but for the admin-specific version of the plugin. Example: `"admin/profile-admin/profile-admin.module"`.
*   `adminAngularModuleName: string | null`: Similar to `angularModuleName`, but for the admin module. Example: `"ProfileAdminModule"`.
*   `adminRoutePath: string | null`: Base route path for the admin version. Example: `"admin/profile-admin"`.
*   `adminNavigationLabel: string | null`: Label for admin navigation.
*   `adminRequiredRole: string | null`: Role required for the admin plugin.

### 3.4. Virtual Module: `plugins:all`

*   **Source:** Generated by `esbuild-plugins.cjs`.
*   **Content:** Exports the `pluginLoaders` object.
    ```typescript
    // Example content of the generated module
    export const pluginLoaders = {
      'portal/profile/profile': () => import(/* path to profile.module.ts */),
      'admin/sample-admin/sample-admin': () => import(/* path to sample-admin.module.ts */),
      // ... other plugins
    };
    ```
*   **Usage:** Imported into `app-routing.module.ts` to provide the functions for lazy-loading plugin modules.

### 3.5. Service: `PluginLoaderService` (`core/services/plugin-loader.service.ts`)

*   **Responsibility:**
    1.  Fetches the array of `AngularPluginInfo` manifests from the `/api/plugins/manifests` backend endpoint.
    2.  Stores these manifests and makes them available to other parts of the application (primarily `AppRoutingModule`).
    3.  Uses a `manifestLoadingPromise` to ensure manifests are loaded before dependent operations (like route generation) proceed.
*   **Invocation:** Its `loadPluginManifests()` method is called via an `APP_INITIALIZER` token, ensuring manifests are fetched early in the application lifecycle.

### 3.6. Routing Module: `AppRoutingModule` (`app-routing.module.ts`)

This is the central hub for dynamic plugin integration on the client side.

*   **`APP_INITIALIZER` Tokens:**
    1.  One token calls `PluginLoaderService.loadPluginManifests()` to fetch manifests.
    2.  A second token (dependent on the first via promise) calls functions to generate and apply the dynamic plugin routes to the Angular `Router`.
*   **Route Generation Functions:**
    *   `generatePluginChildRoutes(injector: Injector): Routes`:
        *   Retrieves manifests from `PluginLoaderService`.
        *   Iterates over manifests suitable for the "portal" section.
        *   **`pluginKey` Generation (Portal):**
            *   Takes `manifest.angularModulePath` (e.g., `"profile/profile.module"`).
            *   Extracts `pluginFolder = manifest.angularModulePath.split('/')[0]` (e.g., `"profile"`).
            *   Constructs `pluginKey = \`portal/\${pluginFolder}/\${pluginFolder}\`` (e.g., `"portal/profile/profile"`). This **must match** the key generated by `esbuild-plugins.cjs`.
        *   Checks if a loader exists in `pluginLoaders` for this `pluginKey`. If not, logs an error and skips the plugin.
        *   Derives `finalRoutePath` from the last segment of `manifest.routePath` (e.g., `"profile"` from `"portal/profile"`).
        *   Constructs a Route object:
            ```typescript
            {
              path: finalRoutePath,
              loadChildren: () => loader().then(m => { /* module validation */ return m[manifest.angularModuleName]; })
                                     .catch(err => { /* error logging */ return PluginLoadErrorComponent; }),
              canActivate: [AuthGuard, RoleGuard],
              data: { /* navigationLabel, requiredRole, etc. */ }
            }
            ```
    *   `generateAdminPluginChildRoutes(injector: Injector): Routes`:
        *   Similar to `generatePluginChildRoutes` but for admin plugins.
        *   Filters manifests that have `adminAngularModulePath`, etc.
        *   **`pluginKey` Generation (Admin):**
            *   Takes `manifest.adminAngularModulePath` (e.g., `"admin/profile-admin/profile-admin.module"`).
            *   Calculates `relativePath` by removing the leading `"admin/"` prefix.
            *   Extracts `pluginFolder = relativePath.split('/')[0]` (e.g., `"profile-admin"`).
            *   Constructs `pluginKey = \`admin/\${pluginFolder}/\${pluginFolder}\`` (e.g., `"admin/profile-admin/profile-admin"`). This **must match** the key from `esbuild-plugins.cjs`.
        *   Constructs admin-specific routes similarly.
    *   `generatePluginHostRoutes(injector: Injector): Route[]`: Creates a parent route (e.g., path `portal`, using `PortalLayoutComponent`) under which all portal plugin child routes are nested.
    *   `generateAdminPluginHostRoutes(injector: Injector): Route[]`: Creates a parent route for admin plugins (e.g., path `admin`, using `AdminLayoutComponent`).
*   **`loadChildren` Logic & Error Handling:**
    *   The `loadChildren` function calls the appropriate loader from the `pluginLoaders` map (e.g., `pluginLoaders[pluginKey]()`).
    *   `.then((m: any) => { ... })`: Once the module `m` is loaded:
        1.  Validates that `manifest.angularModuleName` (or `adminAngularModuleName`) is provided in the manifest. If not, logs an error and returns `PluginLoadErrorComponent`.
        2.  Validates that the loaded module `m` actually exports a member with the name specified by `angularModuleName`. If not, logs an error (including available exports from `m`) and returns `PluginLoadErrorComponent`.
        3.  If valid, returns `m[manifest.angularModuleName]` (the actual Angular module type) to the router.
    *   `.catch((err: any) => { ... })`: If the dynamic `import()` itself fails (e.g., network error, chunk not found):
        1.  Logs the error.
        2.  Returns `PluginLoadErrorComponent`.
*   **Router Configuration:** The generated host routes (with their child plugin routes) are combined with static application routes, and `router.resetConfig()` is called to apply the new comprehensive routing table.

### 3.7. Error Component: `PluginLoadErrorComponent` (`shared/plugins/plugin-load-error/plugin-load-error.component.ts`)

*   **Responsibility:** A standalone Angular component that displays a generic error message.
*   **Usage:** Returned by the `loadChildren` logic in `AppRoutingModule` when a plugin module fails to load (either the chunk is inaccessible or the expected module/export within the chunk is not found). This provides a graceful fallback instead of a broken UI or application crash.

## 4. Workflow

### 4.1. Build-Time Steps

1.  Developer creates a plugin, including its Angular module (e.g., `profile.module.ts`) and ensures its path matches the conventions expected by `esbuild-plugins.cjs` (e.g., within `Frontend/portal/plugin-name/` or `Frontend/admin/plugin-name/`).
2.  During `ng build` (or `ng serve` which invokes a similar build process):
    *   `esbuild-plugins.cjs` scans for plugin modules.
    *   For each valid plugin module found, it generates a `pluginKey` based on its path (e.g., `"portal/profile/profile"`).
    *   It generates the `plugins:all` virtual module, populating the `pluginLoaders` object with these keys and corresponding dynamic `import()` functions.
    *   The main application and plugin chunks are compiled.

### 4.2. Application Initialization (Runtime)

1.  User accesses the GateKeeper Angular application.
2.  `main.ts` bootstraps the `AppModule`.
3.  `AppRoutingModule` is initialized.
4.  The first `APP_INITIALIZER` fires:
    *   `PluginLoaderService.loadPluginManifests()` is called.
    *   An HTTP GET request is made to `/api/plugins/manifests`.
    *   The service stores the fetched manifests and resolves `manifestLoadingPromise`.
5.  The second `APP_INITIALIZER` fires (after manifests are loaded):
    *   `generatePluginHostRoutes()` and `generateAdminPluginHostRoutes()` are called.
    *   These, in turn, call `generatePluginChildRoutes()` and `generateAdminPluginChildRoutes()`.
    *   These functions iterate through the loaded manifests:
        *   For each manifest, they attempt to generate a `pluginKey` using `manifest.angularModulePath` or `manifest.adminAngularModulePath`.
        *   They look up this `pluginKey` in the `pluginLoaders` object (imported from `plugins:all`).
        *   If a loader is found, a new route definition is created for that plugin, configured for lazy loading via `loadChildren`.
    *   The collected plugin routes are merged with static application routes.
    *   `router.resetConfig()` is called to update Angular's live routing configuration.

### 4.3. Navigating to a Plugin Route (Runtime)

1.  User clicks a link or navigates to a URL corresponding to a plugin's route (e.g., `/portal/profile`).
2.  Angular's router matches the route.
3.  The `loadChildren` function associated with that route is executed.
4.  This calls the specific loader function from `pluginLoaders` (e.g., `pluginLoaders['portal/profile/profile']()`).
5.  The browser makes an HTTP request to fetch the JavaScript chunk for that plugin module (if not already cached).
6.  **If successful:**
    *   The JavaScript chunk is loaded and executed.
    *   The promise from `loader().then(m => ...)` resolves with the loaded module `m`.
    *   The code checks for `manifest.angularModuleName` and its presence as an export in `m`.
    *   If valid, `m[manifest.angularModuleName]` (the plugin's Angular Module class) is returned.
    *   Angular router then initializes this module and its components, rendering the plugin's UI.
7.  **If dynamic `import()` fails (e.g., chunk 404, network error):**
    *   The `.catch()` block in `loadChildren` is executed.
    *   The error is logged.
    *   `PluginLoadErrorComponent` is returned.
    *   Angular router renders `PluginLoadErrorComponent`.
8.  **If `import()` succeeds but `angularModuleName` validation fails:**
    *   The relevant error is logged.
    *   `PluginLoadErrorComponent` is returned and rendered.

## 5. `pluginKey` Generation - Critical Alignment

The reliable functioning of this system hinges on the `pluginKey` generated by `esbuild-plugins.cjs` (build-time) perfectly matching the `pluginKey` generated by `AppRoutingModule` (run-time).

*   **Build-Time (`esbuild-plugins.cjs`):**
    *   Regex: `modulePath.match(/Frontend[\/](portal|admin)[\/](?:plugins[\/])?([^\/]+)[\/]/)`
    *   Key: `` `${section}/${pluginFolder}/${pluginFolder}` ``
    *   Example: `Frontend/portal/profile/profile.module.ts` -> `section="portal"`, `pluginFolder="profile"` -> Key: `"portal/profile/profile"`

*   **Run-Time (`AppRoutingModule`):**
    *   **Portal:**
        *   Uses `manifest.angularModulePath` (e.g., `"profile/profile.module"`).
        *   `pluginFolder = manifest.angularModulePath.split('/')[0]` (e.g., `"profile"`).
        *   Key: `` `portal/${pluginFolder}/${pluginFolder}` `` (e.g., `"portal/profile/profile"`).
    *   **Admin:**
        *   Uses `manifest.adminAngularModulePath` (e.g., `"admin/profile-admin/profile-admin.module"`).
        *   `relativePath = manifest.adminAngularModulePath.substring('admin/'.length)` (e.g., `"profile-admin/profile-admin.module"`).
        *   `pluginFolder = relativePath.split('/')[0]` (e.g., `"profile-admin"`).
        *   Key: `` `admin/${pluginFolder}/${pluginFolder}` `` (e.g., `"admin/profile-admin/profile-admin"`).

Any discrepancy between these two generation logics will result in "Loader NOT FOUND" errors, as `AppRoutingModule` will be looking for a key in `pluginLoaders` that doesn't exist.

## 6. Plugin Authoring Guide (Key Considerations)

For a new plugin to be correctly discovered and loaded:

1.  **File Structure:** The plugin's Angular module should reside in a path structure that `esbuild-plugins.cjs` can parse, typically:
    *   Portal: `GATEKEEPER_ROOT/Plugin.Name/Frontend/portal/plugin-folder-name/your-module.module.ts`
    *   Admin: `GATEKEEPER_ROOT/Plugin.Name/Frontend/admin/plugin-folder-name/your-module.module.ts`
    *   The `plugin-folder-name` is critical as it's used in the `pluginKey`.
2.  **Manifest Entry:** A corresponding entry must be served by the `/api/plugins/manifests` endpoint. This entry must accurately provide:
    *   `angularModulePath`: For portal plugins, this should be the path relative to `Frontend/portal/` (or `Frontend/portal/plugins/` if that's part of the convention `esbuild` expects for deriving `pluginFolder`). Example: If plugin module is at `Frontend/portal/myfeature/myfeature.module.ts`, then `angularModulePath` should be `"myfeature/myfeature.module.ts"`.
    *   `angularModuleName`: The exact name of the exported NgModule class from your module file.
    *   `adminAngularModulePath`: For admin plugins, relative to the `Frontend/` directory (including `admin/`). Example: If plugin module is at `Frontend/admin/myadminfeat/myadminfeat.module.ts`, then `adminAngularModulePath` should be `"admin/myadminfeat/myadminfeat.module.ts"`.
    *   `adminAngularModuleName`: For admin modules.
    *   Correct `routePath`, `adminRoutePath`, labels, and roles.
3.  **Module Exports:** The Angular module file specified in `angularModulePath` / `adminAngularModulePath` must export the NgModule class whose name matches `angularModuleName` / `adminAngularModuleName`.

## 7. Troubleshooting / Debugging Tips

*   **"Loader NOT FOUND" errors in browser console (from `AppRoutingModule`):**
    *   Verify the `pluginKey` generated by `AppRoutingModule` (check console logs).
    *   Verify the keys present in the `pluginLoaders` object (check the `plugins:all` virtual module content in browser dev tools or `esbuild` output logs).
    *   The mismatch is likely due to:
        *   Discrepancy in `pluginKey` generation logic between `esbuild-plugins.cjs` and `AppRoutingModule`.
        *   Incorrect `angularModulePath` or `adminAngularModulePath` in the plugin's manifest, leading `AppRoutingModule` to generate a different key than `esbuild` did for that plugin's actual file path.
        *   Plugin files not being correctly discovered by `esbuild-plugins.cjs` (check its console output during build).
*   **"Module export ... not found" errors (from `AppRoutingModule`):**
    *   The plugin chunk was loaded, but the `angularModuleName` (or `adminAngularModuleName`) specified in the manifest does not match any export in the loaded JavaScript module.
    *   Check the manifest's `...ModuleName` field for typos.
    *   Check the plugin's `.module.ts` file to ensure the NgModule class is correctly named and exported.
    *   The console log from `AppRoutingModule` should list available exports, which helps.
*   **404 Errors for JavaScript Chunks:**
    *   The `pluginKey` was matched, and an `import()` was attempted, but the JS file itself is missing from the server/deployment. This is a build or deployment issue.
    *   The `PluginLoadErrorComponent` should be displayed.
*   **Empty `angularModulePath` or `adminAngularModulePath` in Manifest:**
    *   `AppRoutingModule` will log an error and skip the plugin or show `PluginLoadErrorComponent`. Check the manifest data from `/api/plugins/manifests`.
*   **Console Logs are Key:** The system has extensive `console.log` and `console.error` statements. Review these carefully in both the browser and the `esbuild` build output.

## 8. Future Considerations / Potential Improvements

*   **Formal TypeScript Interfaces:** Define `AngularPluginInfo` and other relevant data structures as TypeScript interfaces for better type safety and clarity.
*   **Manifest Validation:** Implement client-side or server-side validation of manifest structures to catch errors early.
*   **Centralized Key Definition:** Instead of `esbuild` deriving the key and `AppRoutingModule` re-deriving it, explore if `esbuild` could output a richer `pluginLoaders` object where each entry also contains the manifest-relevant paths, reducing the need for reconstruction and potential mismatch. This might involve `esbuild` itself reading a simplified form of manifest or plugin metadata at build time.
*   **More Granular Error Reporting:** The `PluginLoadErrorComponent` could potentially accept an error type or message to display slightly more specific (but still user-friendly) information.
*   **Plugin Versioning:** If plugins evolve, managing different versions and compatibility could become a concern.
*   **Shared Dependencies:** Managing shared dependencies between the main app and plugins, or between plugins, to avoid version conflicts or redundant code.
```
