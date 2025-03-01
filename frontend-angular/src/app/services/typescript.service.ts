import { Injectable } from "@angular/core";
import ts from "typescript";
import {
  createDefaultMapFromCDN,
  createSystem,
  createVirtualTypeScriptEnvironment,
} from "@typescript/vfs";

@Injectable({
  providedIn: "root",
})
export class TypeScriptService {
  private fsMap: Map<string, string> | null = null;
  private readonly compilerOpts: ts.CompilerOptions = {
    target: ts.ScriptTarget.ES2015,
  };

  private initializeFsMap() {
    return createDefaultMapFromCDN(this.compilerOpts, "3.7.3", true, ts);
  }

  public async getVirtualTypeScriptEnvironment() {
    if (this.fsMap === null) {
      this.fsMap = await this.initializeFsMap();
    }

    const system = createSystem(this.fsMap);
    return createVirtualTypeScriptEnvironment(
      system,
      [],
      ts,
      this.compilerOpts,
    );
  }
}